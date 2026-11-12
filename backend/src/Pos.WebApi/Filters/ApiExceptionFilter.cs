using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Pos.Application.Common;
using Pos.Application.Common.Exceptions;
using Pos.Domain.Common;

namespace Pos.WebApi.Filters;

/// <summary>
/// Traduce las excepciones de Application, Domain e integración con Odoo a ProblemDetails
/// con el código HTTP correcto. Nunca expone detalles internos ni secretos.
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    /// <summary>Crea el filtro.</summary>
    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        var problem = context.Exception switch
        {
            CustomValidationException ex => FromValidation(ex),
            NotFoundException ex => Problem(StatusCodes.Status404NotFound, ex.Code, ex.Message),
            AppException ex => FromAppException(ex),
            DomainValidationException ex => Problem(StatusCodes.Status400BadRequest, ex.Code, ex.Message),
            PosIntegrationException ex => FromIntegration(ex),
            _ => null
        };

        if (problem is null)
        {
            _logger.LogError(context.Exception, "Excepción no controlada");
            problem = Problem(StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Error interno del servidor.");
        }

        context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
        context.ExceptionHandled = true;
    }

    /// <summary>Código HTTP para cada categoría de <see cref="AppException"/>.</summary>
    public static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.IntegrationUnavailable => StatusCodes.Status503ServiceUnavailable,
        ErrorKind.IntegrationRejected => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.VerificationFailed => StatusCodes.Status409Conflict,
        ErrorKind.Uncertain => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };

    private static ProblemDetails FromValidation(CustomValidationException ex)
    {
        var problem = Problem(StatusCodes.Status400BadRequest, ex.Code, string.Join(" ", ex.ValidationErrors.Distinct()));
        problem.Extensions["errors"] = ex.Errors;
        return problem;
    }

    private static ProblemDetails FromAppException(AppException ex)
    {
        var problem = Problem(StatusFor(ex.Kind), ex.Code, ex.Message);
        if (ex.Details is not null)
            problem.Extensions["details"] = ex.Details;
        return problem;
    }

    private ProblemDetails FromIntegration(PosIntegrationException ex)
    {
        _logger.LogWarning("Falla de integración con Odoo: {Failure} ({OdooError})", ex.Failure, ex.OdooErrorName ?? "-");
        var (status, code, message) = MapIntegration(ex);
        return Problem(status, code, message);
    }

    /// <summary>
    /// RF08: errores de Odoo con mensajes claros. Caída → 503, timeout → 504,
    /// API key o permisos → 502, datos rechazados → 422.
    /// </summary>
    public static (int Status, string Code, string Message) MapIntegration(PosIntegrationException ex) => ex.Failure switch
    {
        PosIntegrationFailure.Unavailable => (StatusCodes.Status503ServiceUnavailable, "ODOO_UNAVAILABLE",
            "Odoo no está disponible en este momento. Verifique que el servidor de Odoo esté encendido e inténtelo de nuevo."),
        PosIntegrationFailure.NotConfigured => (StatusCodes.Status503ServiceUnavailable, "ODOO_NOT_CONFIGURED",
            "La conexión con Odoo no está configurada en el servidor."),
        PosIntegrationFailure.Timeout => (StatusCodes.Status504GatewayTimeout, "ODOO_TIMEOUT",
            "Odoo tardó demasiado en responder. Inténtelo de nuevo."),
        PosIntegrationFailure.Authentication => (StatusCodes.Status502BadGateway, "ODOO_AUTHENTICATION",
            "La API no pudo autenticarse con Odoo. Revise la API key configurada en el servidor."),
        PosIntegrationFailure.Permission => (StatusCodes.Status502BadGateway, "ODOO_PERMISSION",
            "El usuario de integración no tiene permisos en Odoo para esta operación."),
        PosIntegrationFailure.Rejected => (StatusCodes.Status422UnprocessableEntity, "ODOO_REJECTED",
            $"Odoo rechazó la operación: {ex.Message}"),
        PosIntegrationFailure.InvalidResponse => (StatusCodes.Status502BadGateway, "ODOO_INVALID_RESPONSE",
            "Odoo devolvió una respuesta inesperada."),
        _ => (StatusCodes.Status502BadGateway, "ODOO_ERROR", "Error al comunicarse con Odoo.")
    };

    private static ProblemDetails Problem(int status, string code, string detail) => new()
    {
        Status = status,
        Title = code,
        Detail = detail,
        Type = $"https://northwindpos.local/errors/{code.ToLowerInvariant()}"
    };
}
