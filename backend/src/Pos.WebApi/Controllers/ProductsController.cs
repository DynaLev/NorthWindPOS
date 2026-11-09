using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Abstractions;
using Pos.Application.Models;

namespace Pos.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public ProductsController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductInfo>>> Get([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var products = await _catalog.SearchAsync(search, cancellationToken);
        return Ok(products);
    }
}
