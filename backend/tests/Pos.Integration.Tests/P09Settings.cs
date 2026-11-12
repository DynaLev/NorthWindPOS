namespace Pos.Integration.Tests;

/// <summary>
/// Parámetros de P09 por variables de entorno. Los valores por defecto son los del Odoo
/// del proyecto (base odoo20_northwind): sesión 1 "Northwind Caja/00001", pos.config 1,
/// productos 3 (Cafe Norte, NW-CAFE) y 4 (Sandwich Norte, NW-SAND), método Cash id 1.
/// </summary>
internal sealed record P09Settings(
    string? ApiBaseUrl,
    string Username,
    string Password,
    long ProductA,
    long ProductB,
    long? SessionId,
    long? ExpectedCashMethodId,
    string? EvidenceDirectory)
{
    public static P09Settings FromEnvironment() => new(
        Env("P09_API_URL"),
        Env("P09_USER") ?? "cajero",
        Env("P09_PASSWORD") ?? "Cajero123!",
        Long("P09_PRODUCT_A") ?? 3,
        Long("P09_PRODUCT_B") ?? 4,
        Long("P09_SESSION_ID"),
        Long("P09_CASH_METHOD_ID") ?? 1,
        Env("P09_EVIDENCE_DIR"));

    private static string? Env(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    private static long? Long(string name) => long.TryParse(Env(name), out var value) ? value : null;
}
