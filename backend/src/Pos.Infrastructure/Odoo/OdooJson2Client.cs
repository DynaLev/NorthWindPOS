using System.Text.Json;
using Pos.Application.Common;

namespace Pos.Infrastructure.Odoo;

public sealed class OdooJson2Client
{
    private readonly HttpClient _http;
    private readonly OdooOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public OdooJson2Client(HttpClient http, Microsoft.Extensions.Options.IOptions<OdooOptions> options)
    {
        _http = http;
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _options.ApiKey);
        if (!string.IsNullOrWhiteSpace(_options.Database))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Odoo-Database", _options.Database);
        _http.DefaultRequestHeaders.UserAgent.TryParseAdd("NorthWindPOS/1.0");
    }

    public async Task<T> CallAsync<T>(
        string model,
        string method,
        object? body,
        CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/json/2/{model}/{method}";
        var json = JsonSerializer.Serialize(body ?? new { }, JsonOptions);
        try
        {
            using var payload = new StringContent(json, System.Text.Encoding.UTF8);
            payload.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            using var response = await _http.PostAsync(url, payload, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw MapError(response.StatusCode, content);
            }

            if (string.IsNullOrWhiteSpace(content))
                throw new PosIntegrationException(PosIntegrationFailure.InvalidResponse, "Respuesta vacía de Odoo");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<T>(content, options);
            if (result is null)
                throw new PosIntegrationException(PosIntegrationFailure.InvalidResponse, "No se pudo deserializar la respuesta de Odoo");

            return result;
        }
        catch (TaskCanceledException ex)
        {
            throw new PosIntegrationException(PosIntegrationFailure.Timeout, "Tiempo de espera agotado al llamar a Odoo", null, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new PosIntegrationException(PosIntegrationFailure.Unavailable, "Error de conexión con Odoo", null, ex);
        }
        catch (PosIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PosIntegrationException(PosIntegrationFailure.Unexpected, "Error inesperado al llamar a Odoo", null, ex);
        }
    }

    private static PosIntegrationException MapError(System.Net.HttpStatusCode statusCode, string content)
    {
        string? odooName = null;
        string message = $"Odoo devolvió {(int)statusCode} {statusCode}";

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("name", out var nameEl))
                odooName = nameEl.GetString();
            if (doc.RootElement.TryGetProperty("message", out var msgEl))
                message = msgEl.GetString() ?? message;
            else if (doc.RootElement.TryGetProperty("error", out var errEl) && errEl.TryGetProperty("message", out var msg2))
                message = msg2.GetString() ?? message;
        }
        catch { }

        if (statusCode == System.Net.HttpStatusCode.Unauthorized)
            return new PosIntegrationException(PosIntegrationFailure.Authentication, message, odooName);
        if (statusCode == System.Net.HttpStatusCode.Forbidden)
            return new PosIntegrationException(PosIntegrationFailure.Permission, message, odooName);
        if ((int)statusCode >= 400 && (int)statusCode < 500)
            return new PosIntegrationException(PosIntegrationFailure.Rejected, message, odooName);

        return new PosIntegrationException(PosIntegrationFailure.Unavailable, message, odooName);
    }
}
