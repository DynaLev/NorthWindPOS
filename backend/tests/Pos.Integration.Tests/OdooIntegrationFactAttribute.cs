namespace Pos.Integration.Tests;

/// <summary>
/// Prueba que solo corre contra un Odoo 20 real cuando RUN_ODOO_INTEGRATION=1.
/// Sin esa variable aparece como "Skipped" y dotnet test sigue en verde.
/// </summary>
public sealed class OdooIntegrationFactAttribute : FactAttribute
{
    public OdooIntegrationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_ODOO_INTEGRATION") != "1")
        {
            Skip = "P09 contra Odoo real: defina RUN_ODOO_INTEGRATION=1 (ver docs/INTEGRATION_EVIDENCE.md).";
        }
    }
}
