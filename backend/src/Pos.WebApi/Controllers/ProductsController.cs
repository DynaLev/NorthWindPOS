using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Mediator;
using Pos.Application.Features.Products.Queries.SearchProducts;
using Pos.Application.Models;

namespace Pos.WebApi.Controllers;

/// <summary>Catálogo POS (RF02).</summary>
[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Crea el controller.</summary>
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Busca productos vendibles en POS por nombre, código o código de barras.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductInfo>>> Get([FromQuery] string? search, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new SearchProductsQuery(search), cancellationToken));
}
