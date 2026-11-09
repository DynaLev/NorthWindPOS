using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Pos.WebApi.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;

        if (ex is Pos.Application.Common.AppException appEx)
        {
            var status = appEx.Kind switch
            {
                Pos.Application.Common.ErrorKind.Validation => StatusCodes.Status400BadRequest,
                Pos.Application.Common.ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
                Pos.Application.Common.ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
                Pos.Application.Common.ErrorKind.NotFound => StatusCodes.Status404NotFound,
                Pos.Application.Common.ErrorKind.Conflict => StatusCodes.Status409Conflict,
                Pos.Application.Common.ErrorKind.IntegrationUnavailable => StatusCodes.Status503ServiceUnavailable,
                Pos.Application.Common.ErrorKind.IntegrationRejected => StatusCodes.Status422UnprocessableEntity,
                Pos.Application.Common.ErrorKind.VerificationFailed => StatusCodes.Status409Conflict,
                Pos.Application.Common.ErrorKind.Uncertain => StatusCodes.Status504GatewayTimeout,
                Pos.Application.Common.ErrorKind.Unexpected => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };

            var problem = new ProblemDetails
            {
                Status = status,
                Title = appEx.Code,
                Detail = appEx.Message,
                Type = $"https://northwindpos.local/errors/{appEx.Code.ToLowerInvariant()}"
            };
            if (appEx.Details is not null)
            {
                problem.Extensions["details"] = appEx.Details;
            }

            context.Result = new ObjectResult(problem) { StatusCode = status };
            context.ExceptionHandled = true;
            return;
        }

        if (ex is Pos.Domain.Common.DomainValidationException dom)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = dom.Code,
                Detail = dom.Message
            };
            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogError(ex, "Unhandled exception");
        var fallback = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "INTERNAL_ERROR",
            Detail = "Error interno del servidor."
        };
        context.Result = new ObjectResult(fallback) { StatusCode = StatusCodes.Status500InternalServerError };
        context.ExceptionHandled = true;
    }
}
