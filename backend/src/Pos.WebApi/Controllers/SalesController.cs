using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Mediator;
using Pos.Application.Features.Sales.Commands.CreateSale;
using Pos.Application.Features.Sales.Queries.GetSaleById;
using Pos.Application.Features.Sales.Queries.GetSaleByOperationId;
using Pos.Application.Features.Sales.Queries.GetSales;
using Pos.Application.Models;

namespace Pos.WebApi.Controllers;

/// <summary>Ventas en el POS real de Odoo (RF05, RF06, RF07).</summary>
[ApiController]
[Authorize]
[Route("api/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Crea el controller.</summary>
    public SalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registra una venta con pago en efectivo. Idempotente por clientOperationId.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<SaleInfo>> Create([FromBody] CreateSaleCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));

    /// <summary>Historial de ventas.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaleSummaryInfo>>> List([FromQuery] int limit = GetSalesQuery.DefaultLimit, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetSalesQuery(limit), cancellationToken));

    /// <summary>Detalle de una venta por id de pos.order.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<SaleInfo>> Get(long id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetSaleByIdQuery(id), cancellationToken));

    /// <summary>Busca una venta por su identificador de operación (conciliación).</summary>
    [HttpGet("by-operation/{operationId:guid}")]
    public async Task<ActionResult<SaleInfo>> GetByOperation(Guid operationId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetSaleByOperationIdQuery(operationId), cancellationToken));
}
