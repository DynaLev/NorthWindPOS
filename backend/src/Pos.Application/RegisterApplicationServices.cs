using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Common.Mediator;
using Pos.Application.Services;

namespace Pos.Application;

/// <summary>
/// Registro de la capa Application: mediator, handlers, validators y servicios de reglas.
/// </summary>
public static class RegisterApplicationServices
{
    /// <summary>Registra todos los casos de uso de Pos.Application.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMediator, SimpleMediator>();
        services.AddScoped<IPosSessionService, PosSessionService>();
        services.AddSingleton<SaleOperationLock>();

        var assembly = typeof(RegisterApplicationServices).Assembly;
        RegisterImplementationsOf(services, assembly, typeof(IRequestHandler<,>));
        RegisterImplementationsOf(services, assembly, typeof(IValidator<>));

        return services;
    }

    private static void RegisterImplementationsOf(IServiceCollection services, Assembly assembly, Type openGeneric)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var implementation in implementations)
        {
            var contracts = implementation.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
            foreach (var contract in contracts)
            {
                services.AddScoped(contract, implementation);
            }
        }
    }
}
