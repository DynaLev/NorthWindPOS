using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Auth.Commands.Login;

/// <summary>Login del cajero en la API .NET (RF01). Las credenciales de Odoo nunca llegan al navegador.</summary>
/// <param name="Username">Usuario del cajero.</param>
/// <param name="Password">Contraseña del cajero.</param>
public sealed record LoginCommand(string? Username, string? Password) : IRequest<AuthToken>
{
    /// <summary>Evita que la contraseña aparezca en logs.</summary>
    public override string ToString() => $"LoginCommand {{ Username = {Username} }}";
}
