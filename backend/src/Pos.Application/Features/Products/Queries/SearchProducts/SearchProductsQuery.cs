using FluentValidation;
using Pos.Application.Abstractions;
using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Products.Queries.SearchProducts;

/// <summary>Busca productos vendibles en POS por nombre, código o código de barras (RF02).</summary>
/// <param name="Search">Texto a buscar; vacío lista los productos del POS.</param>
public sealed record SearchProductsQuery(string? Search) : IRequest<IReadOnlyList<ProductInfo>>;

/// <summary>Limita el largo del texto de búsqueda.</summary>
public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    /// <summary>Define las reglas de la búsqueda.</summary>
    public SearchProductsQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(100)
            .WithErrorCode("SEARCH_TOO_LONG")
            .WithMessage("La búsqueda no puede superar 100 caracteres.");
    }
}

/// <summary>Consulta el catálogo POS en Odoo.</summary>
public sealed class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, IReadOnlyList<ProductInfo>>
{
    private readonly IPosGateway _gateway;

    /// <summary>Crea el handler.</summary>
    public SearchProductsQueryHandler(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProductInfo>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var term = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        return _gateway.SearchProductsAsync(term, cancellationToken);
    }
}
