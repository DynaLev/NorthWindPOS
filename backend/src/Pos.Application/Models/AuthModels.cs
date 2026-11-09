namespace Pos.Application.Models;

public sealed record LoginRequest(string? Username, string? Password);

public sealed record OperatorInfo(string Username, string DisplayName, string Role);

public sealed record OperatorAccount(
    string Username,
    string DisplayName,
    string Role,
    string PasswordHash);

public sealed record AuthToken(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    OperatorInfo Operator);
