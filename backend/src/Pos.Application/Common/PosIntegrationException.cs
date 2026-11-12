namespace Pos.Application.Common;

/// <summary>
/// Tipo de falla al comunicarse con Odoo.
/// </summary>
public enum PosIntegrationFailure
{
    /// <summary>Falta configuración (URL, base o API key).</summary>
    NotConfigured,
    /// <summary>Odoo no responde o devolvió 5xx (503).</summary>
    Unavailable,
    /// <summary>Se agotó el tiempo de espera (504).</summary>
    Timeout,
    /// <summary>Odoo rechazó la API key (401 de Odoo, 502 hacia el cliente).</summary>
    Authentication,
    /// <summary>El usuario de la API key no tiene permisos (403 de Odoo, 502 hacia el cliente).</summary>
    Permission,
    /// <summary>Odoo rechazó los datos (4xx de Odoo, 422 hacia el cliente).</summary>
    Rejected,
    /// <summary>Respuesta de Odoo vacía o con formato inesperado (502).</summary>
    InvalidResponse,
    /// <summary>Error no clasificado (502).</summary>
    Unexpected
}

/// <summary>
/// Falla al comunicarse con Odoo. La capa Application decide si es un error
/// definitivo o si debe conciliarse para evitar ventas duplicadas.
/// </summary>
public sealed class PosIntegrationException : Exception
{
    /// <summary>Crea la excepción de integración.</summary>
    /// <param name="failure">Tipo de falla.</param>
    /// <param name="message">Mensaje técnico (sin secretos).</param>
    /// <param name="odooErrorName">Nombre de la excepción Python devuelta por Odoo, si existe.</param>
    /// <param name="innerException">Causa original.</param>
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

    /// <summary>Tipo de falla.</summary>
    public PosIntegrationFailure Failure { get; }

    /// <summary>Nombre del error de Odoo (por ejemplo odoo.exceptions.AccessError).</summary>
    public string? OdooErrorName { get; }

    /// <summary>
    /// Indica si la falla es transitoria (conexión o timeout) y una lectura puede reintentarse.
    /// </summary>
    public bool IsTransient => Failure is PosIntegrationFailure.Unavailable or PosIntegrationFailure.Timeout;
}
