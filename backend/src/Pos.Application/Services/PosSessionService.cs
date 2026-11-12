using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Common.Exceptions;
using Pos.Application.Models;

namespace Pos.Application.Services;

/// <summary>
/// Resuelve y valida la sesión POS de Odoo antes de vender (RN04, RN05).
/// </summary>
public sealed class PosSessionService : IPosSessionService
{
    private readonly IPosGateway _gateway;

    /// <summary>Crea el servicio sobre el adaptador de Odoo.</summary>
    public PosSessionService(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public async Task<PosSessionInfo> GetActiveAsync(CancellationToken cancellationToken)
    {
        var session = await _gateway.FindActiveSessionAsync(cancellationToken)
            ?? throw new NotFoundException(
                "POS_SESSION_NOT_FOUND",
                "No hay una sesión POS abierta en Odoo para el POS configurado.");

        Validate(session);
        return session;
    }

    /// <inheritdoc />
    public async Task<PosSessionInfo> ResolveForSaleAsync(
        long? sessionId,
        CancellationToken cancellationToken)
    {
        var session = sessionId is > 0
            ? await _gateway.FindSessionAsync(sessionId.Value, cancellationToken)
            : await _gateway.FindActiveSessionAsync(cancellationToken);

        if (session is null)
        {
            throw new NotFoundException(
                "POS_SESSION_NOT_FOUND",
                "No se encontró una sesión POS válida. Abra una sesión en Odoo e inténtelo nuevamente.");
        }

        Validate(session);
        return session;
    }

    private static void Validate(PosSessionInfo session)
    {
        if (!string.Equals(session.State, "opened", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                ErrorKind.Validation,
                "POS_SESSION_NOT_OPEN",
                "La sesión POS no está abierta. Solo se permite registrar ventas con una sesión abierta.");
        }

        if (session.CashPaymentMethodId is null or <= 0)
        {
            throw new AppException(
                ErrorKind.Validation,
                "POS_CASH_METHOD_MISSING",
                "La configuración POS no tiene un método de pago en efectivo. Configúrelo en Odoo antes de cobrar.");
        }
    }
}
