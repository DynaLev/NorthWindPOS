namespace Pos.Infrastructure.Odoo;

public sealed class OdooOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 1;
}
