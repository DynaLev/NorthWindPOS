using Pos.Application.Models;

namespace Pos.Application.Abstractions;

public interface IAuthenticationService
{
    Task<AuthToken> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public interface IOperatorRepository
{
    Task<OperatorAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    bool Verify(string password, string encodedHash);
}

public interface ITokenIssuer
{
    AuthToken Issue(OperatorAccount account);
}
