namespace Pos.Infrastructure.Odoo;

/// <summary>
/// Configuración de la conexión JSON-2 con Odoo (sección "Odoo").
/// ApiKey se carga desde user-secrets o la variable de entorno Odoo__ApiKey; nunca va al repositorio.
/// </summary>
public sealed class OdooOptions
{
    /// <summary>URL base de Odoo, por ejemplo http://127.0.0.1:8070.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Base de datos (cabecera X-Odoo-Database).</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>API key del usuario de integración (Authorization: Bearer).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de cada llamada HTTP a Odoo.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Reintentos para lecturas seguras (search_read, read, fields_get...) ante caída o timeout.
    /// El envío de la venta (sync_from_ui) nunca se reintenta automáticamente.
    /// </summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>Espera base entre reintentos de lectura, en milisegundos (crece con cada intento).</summary>
    public int RetryDelayMilliseconds { get; set; } = 300;

    /// <summary>
    /// Id de pos.config del punto de venta de esta caja. Si es mayor que cero, solo se usan
    /// sesiones y ventas de ese POS; si es 0 se toma la sesión abierta más reciente de cualquier POS.
    /// </summary>
    public long PosConfigId { get; set; }
}
