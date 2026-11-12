using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Features.Auth.Commands.Login;
using Pos.Application.Models;

namespace Pos.Application.Tests;

/// <summary>RF01: login del cajero.</summary>
public sealed class LoginCommandHandlerTests
{
    private static readonly OperatorAccount Cajero = new("cajero", "Cajero Northwind", "Cashier", "hash-ok");

    private sealed class Operators : IOperatorRepository
    {
        public Task<OperatorAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(string.Equals(username, "cajero", StringComparison.OrdinalIgnoreCase) ? Cajero : null);
    }

    private sealed class Hasher : IPasswordHasher
    {
        public bool Verify(string password, string encodedHash) => password == "Cajero123!" && encodedHash == "hash-ok";
    }

    private sealed class Issuer : ITokenIssuer
    {
        public AuthToken Issue(OperatorAccount account) =>
            new("jwt-de-prueba", DateTimeOffset.UtcNow.AddHours(1), new OperatorInfo(account.Username, account.DisplayName, account.Role));
    }

    private static LoginCommandHandler Handler() => new(new Operators(), new Hasher(), new Issuer());

    [Fact]
    public async Task ValidCredentials_ReturnToken()
    {
        var token = await Handler().Handle(new LoginCommand(" cajero ", "Cajero123!"), CancellationToken.None);
        Assert.Equal("cajero", token.Operator.Username);
        Assert.False(string.IsNullOrEmpty(token.AccessToken));
    }

    [Theory]
    [InlineData("cajero", "incorrecta")]
    [InlineData("otro", "Cajero123!")]
    public async Task InvalidCredentials_AreUnauthorized(string user, string password)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Handler().Handle(new LoginCommand(user, password), CancellationToken.None));
        Assert.Equal(ErrorKind.Unauthorized, ex.Kind);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", ex.Code);
    }

    [Fact]
    public void LoginCommand_ToString_DoesNotLeakPassword()
    {
        Assert.DoesNotContain("Cajero123!", new LoginCommand("cajero", "Cajero123!").ToString());
    }
}
