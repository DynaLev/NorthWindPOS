namespace Pos.Application.Models;

/// <summary>Datos públicos del cajero autenticado.</summary>
/// <param name="Username">Usuario.</param>
/// <param name="DisplayName">Nombre visible.</param>
/// <param name="Role">Rol (por ejemplo Cashier).</param>
public sealed record OperatorInfo(string Username, string DisplayName, string Role);

/// <summary>Cuenta de cajero configurada en el servidor (nunca sale hacia el navegador).</summary>
/// <param name="Username">Usuario.</param>
/// <param name="DisplayName">Nombre visible.</param>
/// <param name="Role">Rol.</param>
/// <param name="PasswordHash">Hash PBKDF2 de la contraseña.</param>
public sealed record OperatorAccount(
    string Username,
    string DisplayName,
    string Role,
    string PasswordHash);

/// <summary>Token JWT emitido por la API .NET tras un login correcto (RF01).</summary>
/// <param name="AccessToken">JWT firmado por la API.</param>
/// <param name="ExpiresAtUtc">Vencimiento del token.</param>
/// <param name="Operator">Cajero autenticado.</param>
public sealed record AuthToken(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    OperatorInfo Operator);
