using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pos.Application.Common;

namespace Pos.Infrastructure.Odoo;

/// <summary>
/// Cliente JSON-2 de Odoo: POST /json/2/&lt;modelo&gt;/&lt;método&gt; con Authorization: Bearer y X-Odoo-Database.
/// Reintenta solo lecturas seguras y registra modelo, método, estado y duración (nunca cabeceras ni API key).
/// </summary>
public sealed class OdooJson2Client
{
    private static readonly HashSet<string> SafeReadMethods = new(StringComparer.Ordinal)
    {
        "search_read", "read", "search", "search_count", "fields_get", "get_product_info_pos"
    };

    private static readonly JsonSerializerOptions RequestJsonOptions = new() { PropertyNamingPolicy = null };
    private static readonly JsonSerializerOptions ResponseJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly OdooOptions _options;
    private readonly ILogger<OdooJson2Client> _logger;

    /// <summary>Crea el cliente y fija las cabeceras de autenticación de Odoo.</summary>
    public OdooJson2Client(HttpClient http, IOptions<OdooOptions> options, ILogger<OdooJson2Client> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", _options.ApiKey);
        if (!string.IsNullOrWhiteSpace(_options.Database))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Odoo-Database", _options.Database);
        _http.DefaultRequestHeaders.UserAgent.TryParseAdd("NorthWindPOS/1.0");
    }

    /// <summary>
    /// Llama a un método de Odoo. Las lecturas seguras se reintentan hasta <see cref="OdooOptions.MaxRetries"/>
    /// veces ante caída o timeout; las escrituras (sync_from_ui) se envían una sola vez.
    /// </summary>
    /// <exception cref="PosIntegrationException">Ante cualquier falla de integración.</exception>
    public async Task<T> CallAsync<T>(string model, string method, object? body, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var maxAttempts = 1 + (SafeReadMethods.Contains(method) ? Math.Max(0, _options.MaxRetries) : 0);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await SendOnceAsync<T>(model, method, body, attempt, cancellationToken);
            }
            catch (PosIntegrationException ex) when (ex.IsTransient && attempt < maxAttempts)
            {
                _logger.LogWarning("Odoo {Model}.{Method}: {Failure}; reintento de lectura {Next}/{Max}.",
                    model, method, ex.Failure, attempt + 1, maxAttempts);
                await Task.Delay(Math.Max(0, _options.RetryDelayMilliseconds) * attempt, cancellationToken);
            }
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new PosIntegrationException(
                PosIntegrationFailure.NotConfigured,
                "Falta configurar Odoo:BaseUrl u Odoo:ApiKey en el servidor.");
        }
    }

    private async Task<T> SendOnceAsync<T>(string model, string method, object? body, int attempt, CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/json/2/{model}/{method}";
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var payload = new StringContent(JsonSerializer.Serialize(body ?? new { }, RequestJsonOptions), Encoding.UTF8);
            payload.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var response = await _http.PostAsync(url, payload, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            LogCall(model, method, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, attempt);

            if (!response.IsSuccessStatusCode)
                throw MapError(response.StatusCode, content);

            return Deserialize<T>(content);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            LogFailure(model, method, PosIntegrationFailure.Timeout, stopwatch.ElapsedMilliseconds, attempt);
            throw new PosIntegrationException(PosIntegrationFailure.Timeout, "Tiempo de espera agotado al llamar a Odoo", null, ex);
        }
        catch (HttpRequestException ex)
        {
            LogFailure(model, method, PosIntegrationFailure.Unavailable, stopwatch.ElapsedMilliseconds, attempt);
            throw new PosIntegrationException(PosIntegrationFailure.Unavailable, "Error de conexión con Odoo", null, ex);
        }
        catch (PosIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogFailure(model, method, PosIntegrationFailure.Unexpected, stopwatch.ElapsedMilliseconds, attempt);
            throw new PosIntegrationException(PosIntegrationFailure.Unexpected, "Error inesperado al llamar a Odoo", null, ex);
        }
    }

    private static T Deserialize<T>(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new PosIntegrationException(PosIntegrationFailure.InvalidResponse, "Respuesta vacía de Odoo");

        try
        {
            return JsonSerializer.Deserialize<T>(content, ResponseJsonOptions)
                ?? throw new PosIntegrationException(PosIntegrationFailure.InvalidResponse, "No se pudo deserializar la respuesta de Odoo");
        }
        catch (JsonException ex)
        {
            throw new PosIntegrationException(PosIntegrationFailure.InvalidResponse, "Odoo devolvió un JSON inválido", null, ex);
        }
    }

    private void LogCall(string model, string method, int statusCode, long elapsedMs, int attempt)
    {
        var level = statusCode < 400 ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(level, "Odoo {Model}.{Method} -> HTTP {StatusCode} en {ElapsedMs} ms (intento {Attempt})",
            model, method, statusCode, elapsedMs, attempt);
    }

    private void LogFailure(string model, string method, PosIntegrationFailure failure, long elapsedMs, int attempt)
    {
        _logger.LogWarning("Odoo {Model}.{Method} -> {Failure} en {ElapsedMs} ms (intento {Attempt})",
            model, method, failure, elapsedMs, attempt);
    }

    private static PosIntegrationException MapError(HttpStatusCode statusCode, string content)
    {
        var (odooName, message) = ReadOdooError(content, $"Odoo devolvió {(int)statusCode} {statusCode}");
        var failure = statusCode switch
        {
            HttpStatusCode.Unauthorized => PosIntegrationFailure.Authentication,
            HttpStatusCode.Forbidden => PosIntegrationFailure.Permission,
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => PosIntegrationFailure.Timeout,
            _ when (int)statusCode is >= 400 and < 500 => PosIntegrationFailure.Rejected,
            _ => PosIntegrationFailure.Unavailable
        };
        return new PosIntegrationException(failure, message, odooName);
    }

    private static (string? Name, string Message) ReadOdooError(string content, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (root.TryGetProperty("message", out var msgEl))
                return (name, msgEl.GetString() ?? fallback);
            if (root.TryGetProperty("error", out var errEl) && errEl.ValueKind == JsonValueKind.Object
                && errEl.TryGetProperty("message", out var msg2))
                return (name, msg2.GetString() ?? fallback);
            return (name, fallback);
        }
        catch (JsonException)
        {
            return (null, fallback);
        }
    }
}
