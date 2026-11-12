using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pos.Integration.Tests;

/// <summary>
/// P09: integración real .NET → Odoo 20 (2 productos + efectivo + reintento sin duplicar).
/// Por defecto levanta la API en proceso con la configuración local (appsettings.Development.json
/// o user-secrets); con P09_API_URL usa una API ya levantada. Nunca guarda el JWT ni la API key.
/// </summary>
public sealed class P09RealOdooTests : IDisposable
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private readonly P09Settings _settings = P09Settings.FromEnvironment();
    private readonly WebApplicationFactory<Program>? _factory;
    private readonly HttpClient _client;

    public P09RealOdooTests()
    {
        if (_settings.ApiBaseUrl is not null)
        {
            _client = new HttpClient { BaseAddress = new Uri(_settings.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(120) };
        }
        else
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
            _client = _factory.CreateClient();
        }
    }

    [OdooIntegrationFact]
    public async Task P09_SaleWithTwoProductsAndCash_IsVerifiedInOdoo_AndRetryDoesNotDuplicate()
    {
        await LoginAsync();

        var products = await GetJsonAsync("/api/products", "p09-01-products.json");
        Assert.Contains(products.AsArray(), p => (long)p!["id"]! == _settings.ProductA);
        Assert.Contains(products.AsArray(), p => (long)p!["id"]! == _settings.ProductB);

        var session = await GetJsonAsync("/api/pos/sessions/active", "p09-02-session.json");
        Assert.Equal("opened", (string?)session["state"]);
        Assert.NotNull(session["cashPaymentMethodId"]);
        var sessionId = _settings.SessionId ?? (long)session["id"]!;

        var operationId = Guid.NewGuid();
        var request = SaleRequest(operationId, sessionId);
        Save("p09-03-sale-request.json", request);

        var first = await PostSaleAsync(request, "p09-04-sale-response.json");
        AssertSaleVerified(first, operationId);
        Assert.False((bool)first["replay"]!);
        var orderId = (long)first["orderId"]!;

        var detail = await GetJsonAsync($"/api/sales/{orderId}", "p09-05-sale-detail.json");
        Assert.Equal((decimal)first["total"]!, (decimal)detail["total"]!);
        Assert.Equal(2, detail["lines"]!.AsArray().Count);

        var retry = await PostSaleAsync(request, "p09-06-retry-same-operation.json");
        Assert.True((bool)retry["replay"]!);
        Assert.Equal(orderId, (long)retry["orderId"]!);

        var history = await GetJsonAsync("/api/sales?limit=200", "p09-07-history.json");
        Assert.Single(history.AsArray(), s => (string?)s!["operationId"] == operationId.ToString());
    }

    [OdooIntegrationFact]
    public async Task P09_TwoSimultaneousPostsWithSameOperation_CreateOneOrder()
    {
        await LoginAsync();
        var session = await GetJsonAsync("/api/pos/sessions/active", null);
        var operationId = Guid.NewGuid();
        var request = SaleRequest(operationId, _settings.SessionId ?? (long)session["id"]!);

        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/sales", request),
            _client.PostAsJsonAsync("/api/sales", request));
        var bodies = new List<JsonNode>();
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            bodies.Add(JsonNode.Parse(await response.Content.ReadAsStringAsync())!);
        }
        Save("p09-08-double-click.json", new JsonArray(bodies.Select(b => b.DeepClone()).ToArray()));

        Assert.Equal((long)bodies[0]["orderId"]!, (long)bodies[1]["orderId"]!);
        Assert.Single(bodies, b => !(bool)b["replay"]!);
    }

    private JsonObject SaleRequest(Guid operationId, long sessionId) => new()
    {
        ["clientOperationId"] = operationId.ToString(),
        ["sessionId"] = sessionId,
        ["lines"] = new JsonArray(
            new JsonObject { ["productId"] = _settings.ProductA, ["quantity"] = 2 },
            new JsonObject { ["productId"] = _settings.ProductB, ["quantity"] = 1 })
    };

    /// <summary>RN06 y RN08: importes y estado leídos de Odoo, pago en efectivo completo.</summary>
    private void AssertSaleVerified(JsonNode sale, Guid operationId)
    {
        Assert.Equal(operationId.ToString(), (string?)sale["operationId"]);
        Assert.Contains((string?)sale["state"], new[] { "paid", "done", "invoiced" });

        var total = (decimal)sale["total"]!;
        var linesTotal = sale["lines"]!.AsArray().Sum(l => (decimal)l!["subtotalWithTax"]!);
        Assert.True(total > 0);
        Assert.Equal(total, linesTotal);
        Assert.True((decimal)sale["paid"]! >= total);

        var payments = sale["payments"]!.AsArray();
        Assert.Contains(payments, p => (bool)p!["isCash"]!);
        if (_settings.ExpectedCashMethodId is { } cashId)
            Assert.Contains(payments, p => (long)p!["paymentMethodId"]! == cashId);
    }

    private async Task LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = _settings.Username, password = _settings.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (string)body["accessToken"]!);
        Save("p09-00-login.json", new JsonObject { ["status"] = 200, ["operator"] = body["operator"]!.DeepClone() });
    }

    private async Task<JsonNode> GetJsonAsync(string path, string? evidenceFile)
    {
        var response = await _client.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"GET {path} → {(int)response.StatusCode}: {content}");
        var node = JsonNode.Parse(content)!;
        if (evidenceFile is not null) Save(evidenceFile, node);
        return node;
    }

    private async Task<JsonNode> PostSaleAsync(JsonObject request, string evidenceFile)
    {
        var response = await _client.PostAsJsonAsync("/api/sales", request);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"POST /api/sales → {(int)response.StatusCode}: {content}");
        var node = JsonNode.Parse(content)!;
        Save(evidenceFile, node);
        return node;
    }

    private void Save(string fileName, JsonNode node)
    {
        if (_settings.EvidenceDirectory is null) return;
        Directory.CreateDirectory(_settings.EvidenceDirectory);
        File.WriteAllText(Path.Combine(_settings.EvidenceDirectory, fileName), node.ToJsonString(Pretty));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory?.Dispose();
    }
}
