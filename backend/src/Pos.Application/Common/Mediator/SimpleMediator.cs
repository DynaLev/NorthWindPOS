using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Common.Exceptions;

namespace Pos.Application.Common.Mediator;

/// <summary>
/// Mediator en proceso, sin dependencias externas: ejecuta los validators
/// registrados para el request y luego su único handler.
/// </summary>
public sealed class SimpleMediator : IMediator
{
    private static readonly MethodInfo SendCoreMethod =
        typeof(SimpleMediator).GetMethod(nameof(SendCoreAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>Crea el mediator sobre el contenedor de dependencias.</summary>
    public SimpleMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var method = SendCoreMethod.MakeGenericMethod(request.GetType(), typeof(TResponse));
        return (Task<TResponse>)method.Invoke(this, [request, cancellationToken])!;
    }

    private async Task<TResponse> SendCoreAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        await ValidateAsync(request, cancellationToken);
        var handler = _serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException($"No hay un handler registrado para {typeof(TRequest).Name}.");
        return await handler.Handle(request, cancellationToken);
    }

    private async Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
    {
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();
        if (validators.Count == 0)
        {
            return;
        }

        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new CustomValidationException(failures);
        }
    }
}
