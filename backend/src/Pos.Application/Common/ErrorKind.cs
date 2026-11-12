namespace Pos.Application.Common;

/// <summary>
/// Categoría de un <see cref="AppException"/>. WebApi la traduce a un código HTTP.
/// </summary>
public enum ErrorKind
{
    /// <summary>Datos inválidos (400).</summary>
    Validation,
    /// <summary>Credenciales inválidas o sesión expirada (401).</summary>
    Unauthorized,
    /// <summary>Operación no permitida (403).</summary>
    Forbidden,
    /// <summary>Recurso inexistente (404).</summary>
    NotFound,
    /// <summary>Conflicto con el estado actual (409).</summary>
    Conflict,
    /// <summary>Odoo no está disponible (503).</summary>
    IntegrationUnavailable,
    /// <summary>Odoo rechazó la operación (422).</summary>
    IntegrationRejected,
    /// <summary>La orden existe en Odoo pero no pudo verificarse como pagada (409).</summary>
    VerificationFailed,
    /// <summary>No se pudo confirmar el resultado de la venta (504).</summary>
    Uncertain,
    /// <summary>Error no previsto (500).</summary>
    Unexpected
}
