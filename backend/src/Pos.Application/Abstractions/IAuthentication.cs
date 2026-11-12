using Pos.Application.Models;

namespace Pos.Application.Abstractions;

/// <summary>Puerto para leer los cajeros configurados en el servidor.</summary>
public interface IOperatorRepository
{
    /// <summary>Busca un cajero por usuario (sin distinguir mayúsculas).</summary>
    Task<OperatorAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
}

/// <summary>Puerto para verificar contraseñas con hash.</summary>
public interface IPasswordHasher
{
    /// <summary>Compara una contraseña con su hash en tiempo constante.</summary>
    bool Verify(string password, string encodedHash);
}

/// <summary>Puerto para emitir el JWT del cajero.</summary>
public interface ITokenIssuer
{
    /// <summary>Emite un token firmado para el cajero.</summary>
    AuthToken Issue(OperatorAccount account);
}
