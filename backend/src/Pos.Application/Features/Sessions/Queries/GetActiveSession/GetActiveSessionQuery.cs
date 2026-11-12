using Pos.Application.Abstractions;
using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Sessions.Queries.GetActiveSession;

/// <summary>Sesión POS abierta del punto de venta configurado (RF04).</summary>
public sealed record GetActiveSessionQuery : IRequest<PosSessionInfo>;

/// <summary>Devuelve la sesión abierta y valida estado y método de efectivo.</summary>
public sealed class GetActiveSessionQueryHandler : IRequestHandler<GetActiveSessionQuery, PosSessionInfo>
{
    private readonly IPosSessionService _sessions;

    /// <summary>Crea el handler.</summary>
    public GetActiveSessionQueryHandler(IPosSessionService sessions)
    {
        _sessions = sessions;
    }

    /// <inheritdoc />
    public Task<PosSessionInfo> Handle(GetActiveSessionQuery request, CancellationToken cancellationToken)
        => _sessions.GetActiveAsync(cancellationToken);
}
