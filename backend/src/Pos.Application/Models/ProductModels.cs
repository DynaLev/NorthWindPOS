namespace Pos.Application.Models;

/// <summary>
/// Producto vendible consultado desde Odoo. Price es el precio base (sin impuestos)
/// informado por Odoo; el importe definitivo se calcula al cobrar (RN06).
/// </summary>
public sealed record ProductInfo(
    long Id,
    string Name,
    string? Code,
    string? Barcode,
    decimal Price,
    string Currency,
    decimal? AvailableQuantity,
    bool TracksInventory,
    string Unit);

/// <summary>
/// Producto con precios POS efectivos calculados por Odoo para una sesión.
/// </summary>
public sealed record SaleProduct(
    long Id,
    string Name,
    bool AvailableInPos,
    decimal UnitPriceWithoutTax,
    decimal UnitPriceWithTax,
    IReadOnlyList<long> TaxIds,
    decimal? AvailableQuantity,
    bool TracksInventory,
    string Unit);
