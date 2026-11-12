using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Abstractions;
using Pos.Infrastructure.Authentication;
using Pos.Infrastructure.Odoo;

namespace Pos.Infrastructure.DependencyInjection;

/// <summary>
/// Registro de los adaptadores de Infrastructure: Odoo (HttpClient + gateway), JWT,
/// hash de contraseñas y repositorio de cajeros. Los casos de uso se registran en
/// Pos.Application (RegisterApplicationServices).
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registra los adaptadores de Infrastructure.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<OdooOptions>(configuration.GetSection("Odoo"));

        services.AddSingleton<IOperatorRepository, ConfiguredOperatorRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        services.AddHttpClient<OdooJson2Client>(client =>
        {
            var options = configuration.GetSection("Odoo").Get<OdooOptions>() ?? new OdooOptions();
            if (options.TimeoutSeconds > 0)
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<IPosGateway, OdooPosGateway>();

        return services;
    }
}
