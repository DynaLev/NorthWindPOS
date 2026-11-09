using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;

namespace Pos.Application.Services;

public sealed class PosSessionService : IPosSessionService
{
    private readonly IPosGateway _gateway;

    public PosSessionService(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<PosSessionInfo> GetActiveAsync(CancellationToken cancellationToken)
    {
        var session = await _gateway.FindActiveSessionAsync(cancellationToken);
        if (session is null)
        {
            throw new AppException(
                ErrorKind.NotFound,
                "POS_SESSION_NOT_FOUND",
                "No hay una sesión POS abierta en Odoo para el POS configurado.");
        }

        Validate(session);
        return session;
    }

    public async Task<PosSessionInfo> ResolveForSaleAsync(
        long? sessionId,
        CancellationToken cancellationToken)
    {
        PosSessionInfo? session = null;
        if (sessionId is > 0)
        {
            session = await _gateway.FindSessionAsync(sessionId.Value, cancellationToken);
        }
        else
        {
            session = await _gateway.FindActiveSessionAsync(cancellationToken);
        }

        if (session is null)
        {
            throw new AppException(
                ErrorKind.NotFound,
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

        if (session.CashPaymentMethodId is null || session.CashPaymentMethodId <= 0)
        {
            throw new AppException(
                ErrorKind.Validation,
                "POS_CASH_METHOD_MISSING",
                "La configuración POS no tiene un método de pago en efectivo. Configúrelo en Odoo antes de cobrar.");
        }
    }
}
