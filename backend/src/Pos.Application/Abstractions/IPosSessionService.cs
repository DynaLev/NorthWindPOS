using Pos.Application.Models;

namespace Pos.Application.Abstractions;

/// <summary>
/// Reglas de sesión POS compartidas por varios casos de uso (RN04, RN05).
/// </summary>
public interface IPosSessionService
{
    /// <summary>Devuelve la sesión abierta del POS configurado o lanza NotFoundException.</summary>
    Task<PosSessionInfo> GetActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resuelve la sesión indicada (o la activa) y comprueba que esté abierta
    /// y tenga un método de pago en efectivo (RN04, RN05).
    /// </summary>
    Task<PosSessionInfo> ResolveForSaleAsync(
        long? sessionId,
        CancellationToken cancellationToken);
}
