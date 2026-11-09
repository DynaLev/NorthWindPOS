using Pos.Application.Models;

namespace Pos.Application.Abstractions;

/// <summary>
/// Adaptador de Odoo. Toda la comunicación con el POS real queda encapsulada
/// en Infrastructure (RNF05).
/// </summary>
public interface IPosGateway
{
    Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken cancellationToken);

    Task<PosSessionInfo?> FindSessionAsync(long sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(
        IReadOnlyCollection<long> productIds,
        PosSessionInfo session,
        CancellationToken cancellationToken);

    Task<SaleInfo?> FindSaleByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken);

    Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Registra la orden en Odoo y devuelve la orden tal como quedó almacenada.
    /// Lanza <see cref="Common.PosIntegrationException"/> ante fallas de integración.
    /// </summary>
    Task<SaleInfo> SubmitSaleAsync(
        SaleSubmission submission,
        CancellationToken cancellationToken);
}
