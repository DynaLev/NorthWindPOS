using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;
using Pos.Domain.Common;
using Pos.Domain.Sales;

namespace Pos.Application.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IOperatorRepository _operators;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenIssuer _issuer;

    public AuthenticationService(
        IOperatorRepository operators,
        IPasswordHasher hasher,
        ITokenIssuer issuer)
    {
        _operators = operators;
        _hasher = hasher;
        _issuer = issuer;
    }

    public async Task<AuthToken> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new AppException(
                ErrorKind.Validation,
                "AUTH_INVALID_CREDENTIALS",
                "Usuario o contraseña inválidos.");
        }

        var account = await _operators.FindByUsernameAsync(username, cancellationToken);
        if (account is null || !_hasher.Verify(password, account.PasswordHash))
        {
            throw new AppException(
                ErrorKind.Validation,
                "AUTH_INVALID_CREDENTIALS",
                "Usuario o contraseña inválidos.");
        }

        return _issuer.Issue(account);
    }
}
