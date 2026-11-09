using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Abstractions;
using Pos.Application.Models;

namespace Pos.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _auth;

    public AuthController(IAuthenticationService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthToken>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _auth.LoginAsync(request ?? new LoginRequest(null, null), cancellationToken);
        return Ok(token);
    }
}
