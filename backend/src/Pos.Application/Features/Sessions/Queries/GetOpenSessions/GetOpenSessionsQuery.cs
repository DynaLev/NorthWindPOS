using Pos.Application.Abstractions;
using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Sessions.Queries.GetOpenSessions;

/// <summary>Sesiones POS abiertas entre las que el cajero puede elegir (RF04).</summary>
public sealed record GetOpenSessionsQuery : IRequest<IReadOnlyList<PosSessionInfo>>;

/// <summary>Lista las sesiones abiertas (filtradas por Odoo:PosConfigId si está configurado).</summary>
public sealed class GetOpenSessionsQueryHandler : IRequestHandler<GetOpenSessionsQuery, IReadOnlyList<PosSessionInfo>>
{
    private readonly IPosGateway _gateway;

    /// <summary>Crea el handler.</summary>
    public GetOpenSessionsQueryHandler(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PosSessionInfo>> Handle(GetOpenSessionsQuery request, CancellationToken cancellationToken)
        => _gateway.ListOpenSessionsAsync(cancellationToken);
}
