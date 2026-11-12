using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Pos.Application.Common;
using Pos.Application.Common.Exceptions;
using Pos.Infrastructure.Authentication;
using Pos.WebApi.Filters;

namespace Pos.WebApi.Tests;

/// <summary>RF08: cada error llega al frontend con el código HTTP correcto (nunca un 500 genérico).</summary>
public sealed class ApiExceptionFilterTests
{
    private static ObjectResult Run(Exception exception)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, []) { Exception = exception };
        new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance).OnException(context);
        Assert.True(context.ExceptionHandled);
        return Assert.IsType<ObjectResult>(context.Result);
    }

    [Theory]
    [InlineData(PosIntegrationFailure.Unavailable, 503, "ODOO_UNAVAILABLE")]
    [InlineData(PosIntegrationFailure.NotConfigured, 503, "ODOO_NOT_CONFIGURED")]
    [InlineData(PosIntegrationFailure.Timeout, 504, "ODOO_TIMEOUT")]
    [InlineData(PosIntegrationFailure.Authentication, 502, "ODOO_AUTHENTICATION")]
    [InlineData(PosIntegrationFailure.Permission, 502, "ODOO_PERMISSION")]
    [InlineData(PosIntegrationFailure.Rejected, 422, "ODOO_REJECTED")]
    [InlineData(PosIntegrationFailure.InvalidResponse, 502, "ODOO_INVALID_RESPONSE")]
    public void PosIntegrationException_IsMapped(PosIntegrationFailure failure, int status, string code)
    {
        var result = Run(new PosIntegrationException(failure, "detalle"));
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(status, result.StatusCode);
        Assert.Equal(code, problem.Title);
    }

    [Fact]
    public void Validation_NotFound_Unauthorized_Uncertain_AreMapped()
    {
        Assert.Equal(400, Run(new CustomValidationException([new ValidationErrorDetail("Lines", "SALE_EMPTY", "Sin líneas")])).StatusCode);
        Assert.Equal(404, Run(new NotFoundException("SALE_NOT_FOUND", "No existe")).StatusCode);
        Assert.Equal(401, Run(new AppException(ErrorKind.Unauthorized, "AUTH_INVALID_CREDENTIALS", "x")).StatusCode);
        Assert.Equal(504, Run(new SaleResultUncertainException(Guid.NewGuid(), "x")).StatusCode);
        Assert.Equal(409, Run(new SaleVerificationException(Guid.NewGuid(), 1, "x")).StatusCode);
    }

    [Fact]
    public void UnknownException_IsGeneric500WithoutDetails()
    {
        var result = Run(new InvalidOperationException("secreto interno"));
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(500, result.StatusCode);
        Assert.DoesNotContain("secreto", problem.Detail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    [InlineData("CAMBIAR_POR_UNA_CLAVE_LARGA_Y_ALEATORIA")]
    public void JwtSigningKey_IsRequiredAtStartup(string key)
    {
        Assert.Throws<InvalidOperationException>(() => JwtStartupValidation.EnsureSigningKey(new JwtOptions { SigningKey = key }));
    }
}
