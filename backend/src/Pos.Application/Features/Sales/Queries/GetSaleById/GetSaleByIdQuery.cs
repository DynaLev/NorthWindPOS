using Pos.Application.Abstractions;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Sales.Queries.GetSaleById;

/// <summary>Detalle de una venta por id de pos.order (RF07).</summary>
/// <param name="Id">Id de la orden en Odoo.</param>
public sealed record GetSaleByIdQuery(long Id) : IRequest<SaleInfo>;

/// <summary>Lee la orden desde Odoo o lanza <see cref="NotFoundException"/>.</summary>
public sealed class GetSaleByIdQueryHandler : IRequestHandler<GetSaleByIdQuery, SaleInfo>
{
    private readonly IPosGateway _gateway;

    /// <summary>Crea el handler.</summary>
    public GetSaleByIdQueryHandler(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public async Task<SaleInfo> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
    {
        var sale = request.Id > 0 ? await _gateway.FindSaleByIdAsync(request.Id, cancellationToken) : null;
        return sale ?? throw new NotFoundException("SALE_NOT_FOUND", "No se encontró la venta solicitada.");
    }
}
