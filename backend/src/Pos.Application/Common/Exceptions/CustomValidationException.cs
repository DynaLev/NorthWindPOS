using FluentValidation.Results;

namespace Pos.Application.Common.Exceptions;

/// <summary>
/// Uno o más validators de FluentValidation rechazaron el request (HTTP 400).
/// </summary>
public sealed class CustomValidationException : Exception
{
    /// <summary>Crea la excepción a partir de los errores de FluentValidation.</summary>
    public CustomValidationException(IEnumerable<ValidationFailure> failures)
        : this(failures.Select(f => new ValidationErrorDetail(f.PropertyName, f.ErrorCode, f.ErrorMessage)).ToList())
    {
    }

    /// <summary>Crea la excepción con errores ya construidos.</summary>
    public CustomValidationException(IReadOnlyList<ValidationErrorDetail> errors)
        : base(errors.Count > 0 ? errors[0].Message : "La solicitud no es válida.")
    {
        Errors = errors;
    }

    /// <summary>Errores por propiedad, con el código de negocio de cada uno.</summary>
    public IReadOnlyList<ValidationErrorDetail> Errors { get; }

    /// <summary>Mensajes de error, como en la práctica CleanTeeth.</summary>
    public IReadOnlyList<string> ValidationErrors => Errors.Select(e => e.Message).ToList();

    /// <summary>Código del primer error (por ejemplo SALE_EMPTY o QUANTITY_INVALID).</summary>
    public string Code => Errors.Count > 0 && !string.IsNullOrWhiteSpace(Errors[0].Code)
        ? Errors[0].Code
        : "VALIDATION_ERROR";
}

/// <summary>Error de validación de una propiedad.</summary>
/// <param name="Property">Propiedad del request (por ejemplo Lines[0].Quantity).</param>
/// <param name="Code">Código de negocio del error.</param>
/// <param name="Message">Mensaje en español para el cajero.</param>
public sealed record ValidationErrorDetail(string Property, string Code, string Message);
