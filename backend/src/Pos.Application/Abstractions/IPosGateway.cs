using Pos.Application.Models;

namespace Pos.Application.Abstractions;

/// <summary>
/// Adaptador de Odoo. Toda la comunicación con el POS real queda encapsulada
/// en Infrastructure (RNF05). Las lecturas pueden reintentarse; el envío de la venta no.
/// </summary>
public interface IPosGateway
{
    /// <summary>Sesión abierta más reciente del POS configurado (Odoo:PosConfigId si existe).</summary>
    Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken cancellationToken);

    /// <summary>Sesiones abiertas que el cajero puede elegir (filtradas por Odoo:PosConfigId si existe).</summary>
    Task<IReadOnlyList<PosSessionInfo>> ListOpenSessionsAsync(CancellationToken cancellationToken);

    /// <summary>Lee una sesión por id; null si no existe.</summary>
    Task<PosSessionInfo?> FindSessionAsync(long sessionId, CancellationToken cancellationToken);

    /// <summary>Busca productos vendibles en POS por nombre, código o código de barras.</summary>
    Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(
        string? search,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lee los productos a vender con los precios POS de Odoo para la cantidad de cada línea.
    /// Hay que pasar la cantidad real: con quantity=1 Odoo redondea el unitario (2.88) y al
    /// multiplicar por 2 queda 5.76, pero Odoo recalcula el impuesto sobre la línea (5.75).
    /// Los ids inexistentes no se devuelven.
    /// </summary>
    /// <param name="productIds">Ids de product.product a leer.</param>
    /// <param name="session">Sesión POS abierta, para pos_config_id.</param>
    /// <param name="quantities">Cantidad por producto (suma de las líneas de ese id).</param>
    /// <param name="cancellationToken">Cancelación.</param>
    Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(
        IReadOnlyCollection<long> productIds,
        PosSessionInfo session,
        IReadOnlyDictionary<long, decimal> quantities,
        CancellationToken cancellationToken);

    /// <summary>Busca la orden por uuid (conciliación, RN07).</summary>
    Task<SaleInfo?> FindSaleByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken);

    /// <summary>Lee una orden por id.</summary>
    Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken cancellationToken);

    /// <summary>Últimas órdenes POS.</summary>
    Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Registra la orden en Odoo (pos.order.sync_from_ui) y devuelve la orden tal como quedó almacenada.
    /// Nunca se reintenta a ciegas: ante fallas lanza <see cref="Common.PosIntegrationException"/>
    /// y el caso de uso concilia por uuid.
    /// </summary>
    Task<SaleInfo> SubmitSaleAsync(
        SaleSubmission submission,
        CancellationToken cancellationToken);
}
