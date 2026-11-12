using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Mediator;
using Pos.Application.Features.Auth.Commands.Login;
using Pos.Application.Models;

namespace Pos.WebApi.Controllers;

/// <summary>Login del cajero (RF01).</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Crea el controller.</summary>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Valida usuario y contraseña y devuelve un JWT. Credenciales inválidas: 401.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthToken>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command, cancellationToken));
}
