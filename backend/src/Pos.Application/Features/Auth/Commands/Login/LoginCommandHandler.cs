using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Auth.Commands.Login;

/// <summary>Verifica la contraseña del cajero y emite el JWT. Credenciales inválidas: 401.</summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthToken>
{
    private readonly IOperatorRepository _operators;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenIssuer _issuer;

    /// <summary>Crea el handler con sus puertos.</summary>
    public LoginCommandHandler(IOperatorRepository operators, IPasswordHasher hasher, ITokenIssuer issuer)
    {
        _operators = operators;
        _hasher = hasher;
        _issuer = issuer;
    }

    /// <inheritdoc />
    public async Task<AuthToken> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username!.Trim();
        var account = await _operators.FindByUsernameAsync(username, cancellationToken);
        if (account is null || !_hasher.Verify(request.Password!, account.PasswordHash))
        {
            throw new AppException(
                ErrorKind.Unauthorized,
                "AUTH_INVALID_CREDENTIALS",
                "Usuario o contraseña inválidos.");
        }

        return _issuer.Issue(account);
    }
}
