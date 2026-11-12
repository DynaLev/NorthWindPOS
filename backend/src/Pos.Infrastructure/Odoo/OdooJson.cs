using System.Text.Json;

namespace Pos.Infrastructure.Odoo;

/// <summary>
/// Lectura tolerante de registros JSON de Odoo (los campos vacíos llegan como false).
/// </summary>
internal static class OdooJson
{
    public static string Text(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) ? TextValue(value) : "";

    public static string? OptionalText(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>Texto de un valor; si es un objeto (campo traducible) devuelve la primera traducción.</summary>
    public static string TextValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? "";
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(prop.Value.GetString()))
                    return prop.Value.GetString() ?? "";
            }
        }
        return "";
    }

    public static decimal Decimal(JsonElement record, string property, decimal fallback = 0m) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : fallback;

    public static decimal? OptionalDecimal(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : null;

    public static bool Bool(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    public static long Id(JsonElement record) => record.GetProperty("id").GetInt64();

    /// <summary>Id de un many2one ([id, "nombre"]) o 0 si está vacío.</summary>
    public static long Many2OneId(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() >= 1
            && value[0].TryGetInt64(out var id) ? id : 0;

    /// <summary>Nombre de un many2one ([id, "nombre"]) o cadena vacía.</summary>
    public static string Many2OneName(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() >= 2
            ? TextValue(value[1]) : "";

    /// <summary>Ids de un one2many o many2many.</summary>
    public static long[] Ids(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt64()).ToArray()
            : [];

    public static DateTimeOffset? Date(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(value.GetString(), out var parsed) ? parsed : null;

    public static IReadOnlyList<JsonElement> Rows(JsonElement result) =>
        result.ValueKind == JsonValueKind.Array ? result.EnumerateArray().ToList() : [];
}
