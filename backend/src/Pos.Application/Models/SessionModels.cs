namespace Pos.Application.Models;

/// <summary>
/// Sesión POS de Odoo. CashPaymentMethodId proviene de la configuración
/// real del POS; nunca se inventa (RN05).
/// </summary>
/// <param name="Id">Id de pos.session.</param>
/// <param name="Name">Nombre de la sesión.</param>
/// <param name="ConfigId">Id de pos.config.</param>
/// <param name="ConfigName">Nombre del punto de venta.</param>
/// <param name="State">Estado en Odoo (opened, closing_control, closed).</param>
/// <param name="CompanyId">Compañía.</param>
/// <param name="Currency">Moneda del POS.</param>
/// <param name="CurrencyDecimalPlaces">Decimales de la moneda (para redondear importes, RN06).</param>
/// <param name="CashPaymentMethodId">Método de pago de tipo cash leído de Odoo.</param>
/// <param name="CashPaymentMethodName">Nombre del método de efectivo.</param>
/// <param name="OpenedAt">Fecha de apertura.</param>
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
