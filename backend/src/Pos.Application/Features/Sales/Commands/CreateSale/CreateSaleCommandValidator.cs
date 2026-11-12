using FluentValidation;
using Pos.Application.Models;
using Pos.Domain.Sales;

namespace Pos.Application.Features.Sales.Commands.CreateSale;

/// <summary>
/// Reglas RN01 y RN02 antes de llamar a Odoo: al menos una línea, ids y cantidades positivas.
/// </summary>
public sealed class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>Define las reglas de la venta.</summary>
    public CreateSaleCommandValidator()
    {
        RuleFor(x => x.Lines)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithErrorCode("SALE_EMPTY")
                .WithMessage("La venta debe contener al menos una línea de producto.")
            .Must(lines => lines!.Count <= SaleDraft.MaxLines)
                .WithErrorCode("SALE_TOO_LARGE")
                .WithMessage($"La venta no puede contener más de {SaleDraft.MaxLines} líneas.");

        RuleForEach(x => x.Lines).SetValidator(new SaleLineRequestValidator());

        RuleFor(x => x.SessionId)
            .GreaterThan(0)
            .When(x => x.SessionId is not null)
            .WithErrorCode("POS_SESSION_INVALID")
            .WithMessage("El identificador de la sesión POS debe ser mayor que cero.");
    }
}

/// <summary>Reglas de cada línea de la venta.</summary>
public sealed class SaleLineRequestValidator : AbstractValidator<SaleLineRequest>
{
    /// <summary>Define las reglas de la línea.</summary>
    public SaleLineRequestValidator()
    {
        RuleFor(l => l.ProductId)
            .GreaterThan(0)
            .WithErrorCode("PRODUCT_ID_INVALID")
            .WithMessage("El identificador del producto debe ser mayor que cero.");

        RuleFor(l => l.Quantity)
            .GreaterThan(0)
                .WithErrorCode("QUANTITY_INVALID")
                .WithMessage("La cantidad de cada línea debe ser mayor que cero.")
            .LessThanOrEqualTo(SaleLine.MaxQuantity)
                .WithErrorCode("QUANTITY_INVALID")
                .WithMessage($"La cantidad de una línea no puede superar {SaleLine.MaxQuantity:N0}.");
    }
}
