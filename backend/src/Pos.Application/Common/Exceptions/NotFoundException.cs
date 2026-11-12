namespace Pos.Application.Common.Exceptions;

/// <summary>
/// El recurso pedido no existe en Odoo (HTTP 404).
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Crea la excepción con un código de negocio y un mensaje para el usuario.</summary>
    public NotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Código de negocio, por ejemplo SALE_NOT_FOUND o POS_SESSION_NOT_FOUND.</summary>
    public string Code { get; }
}
