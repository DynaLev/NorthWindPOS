namespace Pos.Domain.Common;

/// <summary>
/// Error de una regla de negocio propia del POS web (RN01, RN02).
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
