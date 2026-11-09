namespace Pos.Application.Models;

public sealed record SaleLineRequest(long ProductId, decimal Quantity);

public sealed record CreateSaleRequest(
    Guid? ClientOperationId,
    long? SessionId,
    IReadOnlyList<SaleLineRequest>? Lines);

public sealed record SaleSubmissionLine(
    long ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPriceWithoutTax,
    decimal SubtotalWithoutTax,
    decimal SubtotalWithTax,
    IReadOnlyList<long> TaxIds);

/// <summary>
/// Orden que Infrastructure transforma al formato interno de Odoo.
/// Los importes provienen de la información de precios de Odoo.
/// </summary>
public sealed record SaleSubmission(
    Guid OperationId,
    long SessionId,
    long CompanyId,
    decimal TotalWithoutTax,
    decimal TotalTax,
    decimal TotalWithTax,
    long CashPaymentMethodId,
    IReadOnlyList<SaleSubmissionLine> Lines);

public sealed record SaleLineInfo(
    long ProductId,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    decimal SubtotalWithTax);

public sealed record SalePaymentInfo(
    long Id,
    long PaymentMethodId,
    string PaymentMethodName,
    decimal Amount,
    bool IsCash);

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

public sealed record SaleSummaryInfo(
    long OrderId,
    string OrderName,
    Guid OperationId,
    DateTimeOffset? Date,
    decimal Total,
    string State,
    string PaymentMethodName,
    string Currency);
