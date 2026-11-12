using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;
using Pos.Infrastructure.Authentication;

namespace Pos.WebApi.Tests;

/// <summary>
/// Pruebas HTTP en proceso con un doble de Odoo (no es un simulador de entrega:
/// solo reemplaza IPosGateway dentro de la prueba).
/// </summary>
public sealed class ApiHttpTests : IClassFixture<ApiHttpTests.Factory>
{
    private readonly Factory _factory;

    public ApiHttpTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { username = "cajero", password = "mala" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutPassword_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { username = "cajero", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OdooDown_Returns503_NotGeneric500()
    {
        var client = await AuthenticatedClientAsync();
        var response = await client.GetAsync("/api/products?search=cafe");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ODOO_UNAVAILABLE", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task EmptySale_Returns400_WithValidationErrors()
    {
        var client = await AuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/sales", new { clientOperationId = Guid.NewGuid(), lines = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("SALE_EMPTY", body.RootElement.GetProperty("title").GetString());
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Cors_AllowsViteOrigin_Only()
    {
        var client = _factory.CreateClient();
        async Task<bool> Allowed(string origin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            var response = await client.SendAsync(request);
            return response.Headers.Contains("Access-Control-Allow-Origin");
        }

        Assert.True(await Allowed("http://127.0.0.1:5173"));
        Assert.False(await Allowed("http://evil.example"));
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { username = "cajero", password = Factory.Password });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<AuthToken>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public const string Password = "Prueba123!";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:SigningKey", "clave-solo-para-pruebas-automatizadas-0123456789");
            builder.UseSetting("Auth:Operators:0:Username", "cajero");
            builder.UseSetting("Auth:Operators:0:PasswordHash", new Pbkdf2PasswordHasher().Hash(Password));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPosGateway>();
                services.AddScoped<IPosGateway, OdooDownGateway>();
            });
        }
    }

    /// <summary>Doble que simula Odoo apagado.</summary>
    private sealed class OdooDownGateway : IPosGateway
    {
        private static Exception Down() => new PosIntegrationException(PosIntegrationFailure.Unavailable, "Connection refused");
        public Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken ct) => throw Down();
        public Task<IReadOnlyList<PosSessionInfo>> ListOpenSessionsAsync(CancellationToken ct) => throw Down();
        public Task<PosSessionInfo?> FindSessionAsync(long id, CancellationToken ct) => throw Down();
        public Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string? s, CancellationToken ct) => throw Down();
        public Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(IReadOnlyCollection<long> ids, PosSessionInfo s, IReadOnlyDictionary<long, decimal> q, CancellationToken ct) => throw Down();
        public Task<SaleInfo?> FindSaleByOperationIdAsync(Guid id, CancellationToken ct) => throw Down();
        public Task<SaleInfo?> FindSaleByIdAsync(long id, CancellationToken ct) => throw Down();
        public Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken ct) => throw Down();
        public Task<SaleInfo> SubmitSaleAsync(SaleSubmission s, CancellationToken ct) => throw Down();
    }
}
