namespace Pos.Domain.Common;

/// <summary>
/// Error de una regla de negocio propia del POS web (RN01, RN02).
/// </summary>
public sealed class DomainValidationException : Exception
{
    /// <summary>Crea el error con su código de negocio.</summary>
    public DomainValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Código de negocio (SALE_EMPTY, QUANTITY_INVALID...).</summary>
    public string Code { get; }
}
