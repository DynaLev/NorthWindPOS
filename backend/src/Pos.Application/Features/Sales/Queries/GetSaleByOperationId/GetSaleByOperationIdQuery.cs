using Pos.Application.Abstractions;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Sales.Queries.GetSaleByOperationId;

/// <summary>Busca una venta por su identificador de operación (uuid), para conciliar (RN07).</summary>
/// <param name="OperationId">clientOperationId enviado al cobrar.</param>
public sealed record GetSaleByOperationIdQuery(Guid OperationId) : IRequest<SaleInfo>;

/// <summary>Lee la orden por uuid o lanza <see cref="NotFoundException"/>.</summary>
public sealed class GetSaleByOperationIdQueryHandler : IRequestHandler<GetSaleByOperationIdQuery, SaleInfo>
{
    private readonly IPosGateway _gateway;

    /// <summary>Crea el handler.</summary>
    public GetSaleByOperationIdQueryHandler(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public async Task<SaleInfo> Handle(GetSaleByOperationIdQuery request, CancellationToken cancellationToken)
    {
        var sale = await _gateway.FindSaleByOperationIdAsync(request.OperationId, cancellationToken);
        return sale ?? throw new NotFoundException(
            "SALE_NOT_FOUND",
            "No se encontró una venta con ese identificador de operación.");
    }
}
