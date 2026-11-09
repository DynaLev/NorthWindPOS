using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Models;
using Pos.Application.Services;

namespace Pos.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly SaleService _saleService;

    public SalesController(SaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpPost]
    public async Task<ActionResult<SaleInfo>> Create([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var result = await _saleService.CreateAsync(request ?? new CreateSaleRequest(null, null, null), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaleSummaryInfo>>> List([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _saleService.ListAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SaleInfo>> Get(long id, CancellationToken cancellationToken)
    {
        var result = await _saleService.GetAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-operation/{operationId:guid}")]
    public async Task<ActionResult<SaleInfo>> GetByOperation(Guid operationId, CancellationToken cancellationToken)
    {
        var result = await _saleService.GetByOperationIdAsync(operationId, cancellationToken);
        return Ok(result);
    }
}
