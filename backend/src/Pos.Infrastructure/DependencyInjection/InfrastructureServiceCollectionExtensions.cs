using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pos.Application.Abstractions;
using Pos.Application.Services;
using Pos.Infrastructure.Authentication;
using Pos.Infrastructure.Odoo;

namespace Pos.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<OdooOptions>(configuration.GetSection("Odoo"));

        services.AddSingleton<IOperatorRepository, ConfiguredOperatorRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        services.AddHttpClient<OdooJson2Client>(client =>
        {
            var opts = configuration.GetSection("Odoo").Get<OdooOptions>() ?? new OdooOptions();
            if (opts.TimeoutSeconds > 0)
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        services.AddScoped<IPosGateway, OdooPosGateway>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IPosSessionService, PosSessionService>();
        services.AddScoped<SaleService>();

        return services;
    }
}
