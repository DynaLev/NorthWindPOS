using Pos.Application.Common;
using Pos.Application.Models;
using Pos.Domain.Sales;

namespace Pos.Application.Features.Sales.Common;

/// <summary>
/// Arma la orden para Odoo con los precios que informó Odoo (RN03, RN06),
/// redondeando cada importe a los decimales de la moneda de la sesión.
/// </summary>
public static class SaleSubmissionBuilder
{
    /// <summary>Construye la orden o lanza PRODUCT_NOT_VENDIBLE si algún producto no se puede vender.</summary>
    public static SaleSubmission Build(SaleDraft draft, PosSessionInfo session, IReadOnlyList<SaleProduct> products)
    {
        var productsById = products.ToDictionary(p => p.Id);
        var decimals = Math.Clamp(session.CurrencyDecimalPlaces, 0, 6);
        var lines = draft.Lines.Select(line => BuildLine(line, productsById, decimals)).ToList();

        var totalWithoutTax = lines.Sum(l => l.SubtotalWithoutTax);
        var totalWithTax = lines.Sum(l => l.SubtotalWithTax);

        return new SaleSubmission(
            draft.OperationId,
            session.Id,
            session.CompanyId,
            totalWithoutTax,
            totalWithTax - totalWithoutTax,
            totalWithTax,
            session.CashPaymentMethodId!.Value,
            lines);
    }

    /// <summary>Redondeo comercial (mitad hacia arriba), como Odoo.</summary>
    public static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    private static SaleSubmissionLine BuildLine(SaleLine line, Dictionary<long, SaleProduct> productsById, int decimals)
    {
        var product = EnsureVendible(line.ProductId, productsById);
        return new SaleSubmissionLine(
            product.Id,
            product.Name,
            line.Quantity,
            product.UnitPriceWithoutTax,
            Round(product.UnitPriceWithoutTax * line.Quantity, decimals),
            Round(product.UnitPriceWithTax * line.Quantity, decimals),
            product.TaxIds);
    }

    private static SaleProduct EnsureVendible(long productId, Dictionary<long, SaleProduct> productsById)
    {
        if (!productsById.TryGetValue(productId, out var product))
        {
            throw new AppException(
                ErrorKind.Validation,
                "PRODUCT_NOT_VENDIBLE",
                $"El producto {productId} no es vendible en este POS o no fue encontrado.");
        }

        if (!product.AvailableInPos)
        {
            throw new AppException(
                ErrorKind.Validation,
                "PRODUCT_NOT_VENDIBLE",
                $"El producto '{product.Name}' no está habilitado para vender en este POS.");
        }

        return product;
    }
}
