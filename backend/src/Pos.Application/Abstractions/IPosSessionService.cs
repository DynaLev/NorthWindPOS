using Pos.Application.Models;

namespace Pos.Application.Abstractions;

public interface IPosSessionService
{
    Task<PosSessionInfo> GetActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resuelve la sesión indicada (o la activa) y comprueba que esté abierta
    /// y tenga un método de pago en efectivo (RN04, RN05).
    /// </summary>
    Task<PosSessionInfo> ResolveForSaleAsync(
        long? sessionId,
        CancellationToken cancellationToken);
}

public interface ICatalogService
{
    Task<IReadOnlyList<ProductInfo>> SearchAsync(
        string? search,
        CancellationToken cancellationToken);
}
