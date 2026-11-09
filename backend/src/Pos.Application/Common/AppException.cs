namespace Pos.Application.Common;

/// <summary>
/// Error controlado de la capa Application. WebApi lo traduce a ProblemDetails.
/// </summary>
public class AppException : Exception
{
    public AppException(
        ErrorKind kind,
        string code,
        string message,
        object? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        Code = code;
        Details = details;
    }

    public ErrorKind Kind { get; }

    public string Code { get; }

    public object? Details { get; }
}

/// <summary>
/// El resultado de la venta no pudo confirmarse. Nunca debe mostrarse como exitosa.
/// El mismo OperationId debe reutilizarse para conciliar.
/// </summary>
public sealed class SaleResultUncertainException : AppException
{
    public SaleResultUncertainException(Guid operationId, string message, Exception? innerException = null)
        : base(
            ErrorKind.Uncertain,
            "SALE_RESULT_UNCERTAIN",
            message,
            new { operationId },
            innerException)
    {
        OperationId = operationId;
    }

    public Guid OperationId { get; }
}

/// <summary>
/// Odoo devolvió una orden, pero no cumple las condiciones para informarla como pagada.
/// </summary>
public sealed class SaleVerificationException : AppException
{
    public SaleVerificationException(Guid operationId, long? orderId, string message)
        : base(
            ErrorKind.VerificationFailed,
            "SALE_NOT_VERIFIED",
            message,
            new { operationId, orderId })
    {
        OperationId = operationId;
        OrderId = orderId;
    }

    public Guid OperationId { get; }

    public long? OrderId { get; }
}
