namespace Pos.Application.Common;

/// <summary>
/// Error controlado de la capa Application. WebApi lo traduce a ProblemDetails.
/// </summary>
public class AppException : Exception
{
    /// <summary>Crea un error controlado.</summary>
    /// <param name="kind">Categoría que define el código HTTP.</param>
    /// <param name="code">Código de negocio estable (por ejemplo AUTH_INVALID_CREDENTIALS).</param>
    /// <param name="message">Mensaje en español para el cajero.</param>
    /// <param name="details">Datos adicionales seguros de exponer.</param>
    /// <param name="innerException">Causa original.</param>
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

    /// <summary>Categoría del error.</summary>
    public ErrorKind Kind { get; }

    /// <summary>Código de negocio estable.</summary>
    public string Code { get; }

    /// <summary>Datos adicionales seguros de exponer al cliente.</summary>
    public object? Details { get; }
}

/// <summary>
/// El resultado de la venta no pudo confirmarse. Nunca debe mostrarse como exitosa.
/// El mismo OperationId debe reutilizarse para conciliar.
/// </summary>
public sealed class SaleResultUncertainException : AppException
{
    /// <summary>Crea el error con el identificador de operación a reutilizar.</summary>
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

    /// <summary>Identificador de operación (uuid de la orden en Odoo).</summary>
    public Guid OperationId { get; }
}

/// <summary>
/// Odoo devolvió una orden, pero no cumple las condiciones para informarla como pagada (RN08).
/// </summary>
public sealed class SaleVerificationException : AppException
{
    /// <summary>Crea el error con la orden que no pudo verificarse.</summary>
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

    /// <summary>Identificador de operación (uuid).</summary>
    public Guid OperationId { get; }

    /// <summary>Id de la orden en Odoo, si existe.</summary>
    public long? OrderId { get; }
}
