using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Abstractions;
using Pos.Application.Models;

namespace Pos.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/pos/sessions")]
public sealed class PosSessionsController : ControllerBase
{
    private readonly IPosSessionService _sessions;

    public PosSessionsController(IPosSessionService sessions)
    {
        _sessions = sessions;
    }

    [HttpGet("active")]
    public async Task<ActionResult<PosSessionInfo>> GetActive(CancellationToken cancellationToken)
    {
        var session = await _sessions.GetActiveAsync(cancellationToken);
        return Ok(session);
    }
}
