using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Mediator;
using Pos.Application.Features.Sessions.Queries.GetActiveSession;
using Pos.Application.Features.Sessions.Queries.GetOpenSessions;
using Pos.Application.Models;

namespace Pos.WebApi.Controllers;

/// <summary>Sesiones POS de Odoo (RF04).</summary>
[ApiController]
[Authorize]
[Route("api/pos/sessions")]
public sealed class PosSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Crea el controller.</summary>
    public PosSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Sesión abierta del POS configurado, validada (estado y método de efectivo).</summary>
    [HttpGet("active")]
    public async Task<ActionResult<PosSessionInfo>> GetActive(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetActiveSessionQuery(), cancellationToken));

    /// <summary>Sesiones abiertas para que el cajero elija.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PosSessionInfo>>> GetOpen(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetOpenSessionsQuery(), cancellationToken));
}
