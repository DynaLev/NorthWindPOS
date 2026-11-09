namespace Pos.Application.Common;

public enum PosIntegrationFailure
{
    NotConfigured,
    Unavailable,
    Timeout,
    Authentication,
    Permission,
    Rejected,
    InvalidResponse,
    Unexpected
}

/// <summary>
/// Falla al comunicarse con Odoo. La capa Application decide si es un error
/// definitivo o si debe conciliarse para evitar ventas duplicadas.
/// </summary>
public sealed class PosIntegrationException : Exception
{
    public PosIntegrationException(
        PosIntegrationFailure failure,
        string message,
        string? odooErrorName = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
        OdooErrorName = odooErrorName;
    }

    public PosIntegrationFailure Failure { get; }

    public string? OdooErrorName { get; }
}
