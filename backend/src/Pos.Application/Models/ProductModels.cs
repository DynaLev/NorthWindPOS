namespace Pos.Application.Models;

/// <summary>
/// Producto vendible consultado desde Odoo (DTO de salida de GET /api/products).
/// Price es el precio base sin impuestos; el importe definitivo lo calcula Odoo al cobrar (RN06).
/// </summary>
/// <param name="Id">Id de la variante (product.product).</param>
/// <param name="Name">Nombre.</param>
/// <param name="Code">Referencia interna (default_code).</param>
/// <param name="Barcode">Código de barras.</param>
/// <param name="Price">Precio de lista sin impuestos (lst_price).</param>
/// <param name="Currency">Moneda (código ISO), si Odoo la informa.</param>
/// <param name="AvailableQuantity">Stock disponible (qty_available) o null si no se controla.</param>
/// <param name="TracksInventory">Indica si el producto controla inventario (is_storable).</param>
/// <param name="Unit">Unidad de medida.</param>
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
/// <param name="Id">Id de la variante.</param>
/// <param name="Name">Nombre.</param>
/// <param name="AvailableInPos">True si es vendible (sale_ok) y está habilitado en el POS (available_in_pos).</param>
/// <param name="UnitPriceWithoutTax">Precio unitario sin impuestos según Odoo.</param>
/// <param name="UnitPriceWithTax">Precio unitario con impuestos según Odoo.</param>
/// <param name="TaxIds">Impuestos aplicados.</param>
/// <param name="AvailableQuantity">Stock disponible, si se conoce.</param>
/// <param name="TracksInventory">Indica si controla inventario.</param>
/// <param name="Unit">Unidad de medida.</param>
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
