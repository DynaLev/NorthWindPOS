namespace Pos.Application.Models;

/// <summary>Línea pedida por el frontend. El precio no viaja: lo define Odoo (RN06).</summary>
/// <param name="ProductId">Id de product.product.</param>
/// <param name="Quantity">Cantidad (&gt; 0).</param>
public sealed record SaleLineRequest(long ProductId, decimal Quantity);

/// <summary>Línea que Infrastructure transforma al formato de pos.order.line.</summary>
/// <param name="ProductId">Id de product.product.</param>
/// <param name="ProductName">Nombre.</param>
/// <param name="Quantity">Cantidad.</param>
/// <param name="UnitPriceWithoutTax">Precio unitario sin impuestos según Odoo.</param>
/// <param name="SubtotalWithoutTax">Subtotal sin impuestos, redondeado a la moneda.</param>
/// <param name="SubtotalWithTax">Subtotal con impuestos, redondeado a la moneda.</param>
/// <param name="TaxIds">Impuestos.</param>
public sealed record SaleSubmissionLine(
    long ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPriceWithoutTax,
    decimal SubtotalWithoutTax,
    decimal SubtotalWithTax,
    IReadOnlyList<long> TaxIds);

/// <summary>
/// Orden que Infrastructure transforma al formato de pos.order.sync_from_ui.
/// Los importes provienen de la información de precios de Odoo.
/// </summary>
/// <param name="OperationId">uuid de la orden (idempotencia, RN07).</param>
/// <param name="SessionId">Sesión POS abierta.</param>
/// <param name="CompanyId">Compañía.</param>
/// <param name="TotalWithoutTax">Total sin impuestos.</param>
/// <param name="TotalTax">Impuestos.</param>
/// <param name="TotalWithTax">Total con impuestos (importe del pago en efectivo).</param>
/// <param name="CashPaymentMethodId">Método de pago en efectivo leído de Odoo (RN05).</param>
/// <param name="Lines">Líneas.</param>
public sealed record SaleSubmission(
    Guid OperationId,
    long SessionId,
    long CompanyId,
    decimal TotalWithoutTax,
    decimal TotalTax,
    decimal TotalWithTax,
    long CashPaymentMethodId,
    IReadOnlyList<SaleSubmissionLine> Lines);

/// <summary>Línea de una orden tal como quedó en Odoo.</summary>
/// <param name="ProductId">Producto.</param>
/// <param name="Name">Nombre del producto.</param>
/// <param name="Quantity">Cantidad.</param>
/// <param name="UnitPrice">Precio unitario.</param>
/// <param name="Subtotal">Subtotal sin impuestos.</param>
/// <param name="SubtotalWithTax">Subtotal con impuestos.</param>
public sealed record SaleLineInfo(
    long ProductId,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    decimal SubtotalWithTax);

/// <summary>Pago (pos.payment) de una orden.</summary>
/// <param name="Id">Id del pago.</param>
/// <param name="PaymentMethodId">Método de pago.</param>
/// <param name="PaymentMethodName">Nombre del método.</param>
/// <param name="Amount">Importe.</param>
/// <param name="IsCash">True si el método es de tipo cash en Odoo.</param>
public sealed record SalePaymentInfo(
    long Id,
    long PaymentMethodId,
    string PaymentMethodName,
    decimal Amount,
    bool IsCash);

/// <summary>Venta leída de Odoo (DTO de salida de /api/sales).</summary>
/// <param name="OrderId">Id de pos.order.</param>
/// <param name="OrderName">Nombre de la orden.</param>
/// <param name="OperationId">uuid de la orden.</param>
/// <param name="State">Estado en Odoo.</param>
/// <param name="Date">Fecha de la orden.</param>
/// <param name="Total">amount_total de Odoo.</param>
/// <param name="Tax">amount_tax de Odoo.</param>
/// <param name="Paid">amount_paid de Odoo.</param>
/// <param name="Returned">amount_return (cambio).</param>
/// <param name="Currency">Moneda.</param>
/// <param name="SessionId">Sesión.</param>
/// <param name="ConfigId">Punto de venta.</param>
/// <param name="PaymentMethodName">Método de pago principal.</param>
/// <param name="Lines">Líneas.</param>
/// <param name="Payments">Pagos.</param>
/// <param name="Replay">True si la operación ya existía y no se volvió a registrar.</param>
public sealed record SaleInfo(
    long OrderId,
    string OrderName,
    Guid OperationId,
    string State,
    DateTimeOffset? Date,
    decimal Total,
    decimal Tax,
    decimal Paid,
    decimal Returned,
    string Currency,
    long SessionId,
    long ConfigId,
    string PaymentMethodName,
    IReadOnlyList<SaleLineInfo> Lines,
    IReadOnlyList<SalePaymentInfo> Payments,
    bool Replay = false);

/// <summary>Resumen de venta para el historial.</summary>
/// <param name="OrderId">Id de pos.order.</param>
/// <param name="OrderName">Nombre.</param>
/// <param name="OperationId">uuid.</param>
/// <param name="Date">Fecha.</param>
/// <param name="Total">Total de Odoo.</param>
/// <param name="State">Estado.</param>
/// <param name="PaymentMethodName">Método de pago.</param>
/// <param name="Currency">Moneda.</param>
public sealed record SaleSummaryInfo(
    long OrderId,
    string OrderName,
    Guid OperationId,
    DateTimeOffset? Date,
    decimal Total,
    string State,
    string PaymentMethodName,
    string Currency);
