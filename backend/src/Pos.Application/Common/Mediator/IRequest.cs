namespace Pos.Application.Common.Mediator;

/// <summary>
/// Marca un Command o Query que devuelve <typeparamref name="TResponse"/> (estilo CleanTeeth).
/// </summary>
/// <typeparam name="TResponse">Tipo de respuesta del caso de uso.</typeparam>
public interface IRequest<TResponse>
{
}

/// <summary>
/// Caso de uso que atiende un <typeparamref name="TRequest"/>.
/// </summary>
/// <typeparam name="TRequest">Command o Query atendido.</typeparam>
/// <typeparam name="TResponse">Respuesta del caso de uso.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Ejecuta el caso de uso.</summary>
    /// <param name="request">Datos de entrada ya validados por el mediator.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Punto único por el que WebApi ejecuta casos de uso.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Valida el request con sus validators (FluentValidation) y lo envía a su handler.
    /// </summary>
    /// <exception cref="Exceptions.CustomValidationException">Si algún validator falla.</exception>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
