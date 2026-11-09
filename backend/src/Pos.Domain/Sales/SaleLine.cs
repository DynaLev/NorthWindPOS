using Pos.Domain.Common;

namespace Pos.Domain.Sales;

/// <summary>
/// Línea de una venta. El precio no se guarda aquí: Odoo es la fuente
/// de verdad de precios e impuestos (RN06).
/// </summary>
public sealed class SaleLine
{
    public const decimal MaxQuantity = 100_000m;

    private SaleLine(long productId, decimal quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    public long ProductId { get; }

    public decimal Quantity { get; }

    public static SaleLine Create(long productId, decimal quantity)
    {
        if (productId <= 0)
        {
            throw new DomainValidationException(
                "PRODUCT_ID_INVALID",
                "El identificador del producto debe ser mayor que cero.");
        }

        if (quantity <= 0)
        {
            throw new DomainValidationException(
                "QUANTITY_INVALID",
                "La cantidad de cada línea debe ser mayor que cero.");
        }

        if (quantity > MaxQuantity)
        {
            throw new DomainValidationException(
                "QUANTITY_INVALID",
                $"La cantidad de una línea no puede superar {MaxQuantity:N0}.");
        }

        return new SaleLine(productId, quantity);
    }
}
