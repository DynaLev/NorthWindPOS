using Pos.Domain.Common;

namespace Pos.Domain.Sales;

/// <summary>
/// Línea de una venta. El precio no se guarda aquí: Odoo es la fuente
/// de verdad de precios e impuestos (RN06).
/// </summary>
public sealed class SaleLine
{
    /// <summary>Cantidad máxima por línea.</summary>
    public const decimal MaxQuantity = 100_000m;

    private SaleLine(long productId, decimal quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    /// <summary>Id de product.product en Odoo.</summary>
    public long ProductId { get; }

    /// <summary>Cantidad (&gt; 0).</summary>
    public decimal Quantity { get; }

    /// <summary>Crea la línea o lanza <see cref="DomainValidationException"/> (RN02).</summary>
    /// <param name="productId">Id del producto (&gt; 0).</param>
    /// <param name="quantity">Cantidad (&gt; 0).</param>
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
