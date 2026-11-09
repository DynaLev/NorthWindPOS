using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;

namespace Pos.Application.Services;

public sealed class CatalogService : ICatalogService
{
    private readonly IPosGateway _gateway;

    public CatalogService(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<IReadOnlyList<ProductInfo>> SearchAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        return await _gateway.SearchProductsAsync(search?.Trim(), cancellationToken);
    }
}
