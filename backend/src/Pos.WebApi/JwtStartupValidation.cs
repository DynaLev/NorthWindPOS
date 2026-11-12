using Pos.Infrastructure.Authentication;

namespace Pos.WebApi;

/// <summary>
/// Exige la clave JWT al arrancar en todos los entornos (antes, en Development se generaba
/// una clave solo para validar y el login fallaba al emitir el token).
/// </summary>
public static class JwtStartupValidation
{
    /// <summary>Largo mínimo de la clave HS256 (256 bits).</summary>
    public const int MinimumKeyLength = 32;

    /// <summary>Lanza InvalidOperationException si falta Jwt:SigningKey, es demasiado corta o es el texto de ejemplo.</summary>
    public static void EnsureSigningKey(JwtOptions options)
    {
        var key = options.SigningKey ?? string.Empty;
        var isPlaceholder = key.StartsWith("CAMBIAR", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("USAR_", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(key) || key.Length < MinimumKeyLength || isPlaceholder)
        {
            throw new InvalidOperationException(
                $"Configure Jwt:SigningKey (mínimo {MinimumKeyLength} caracteres) con 'dotnet user-secrets set \"Jwt:SigningKey\" \"...\"' " +
                "o con la variable de entorno Jwt__SigningKey.");
        }
    }
}
