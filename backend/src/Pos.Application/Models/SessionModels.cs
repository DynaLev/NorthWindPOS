namespace Pos.Application.Models;

/// <summary>
/// Sesión POS abierta en Odoo. CashPaymentMethodId proviene de la configuración
/// real del POS; nunca se inventa (RN05).
/// </summary>
public sealed record PosSessionInfo(
    long Id,
    string Name,
    long ConfigId,
    string ConfigName,
    string State,
    long CompanyId,
    string Currency,
    int CurrencyDecimalPlaces,
    long? CashPaymentMethodId,
    string? CashPaymentMethodName,
    DateTimeOffset? OpenedAt);
