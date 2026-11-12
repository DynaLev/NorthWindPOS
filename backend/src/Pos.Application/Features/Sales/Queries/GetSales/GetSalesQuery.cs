using Pos.Application.Abstractions;
using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Sales.Queries.GetSales;

/// <summary>Historial de ventas POS (RF07).</summary>
/// <param name="Limit">Cantidad máxima (1 a 200; por defecto 50).</param>
public sealed record GetSalesQuery(int Limit = GetSalesQuery.DefaultLimit) : IRequest<IReadOnlyList<SaleSummaryInfo>>
{
    /// <summary>Límite por defecto.</summary>
    public const int DefaultLimit = 50;

    /// <summary>Límite máximo permitido.</summary>
    public const int MaxLimit = 200;
}

/// <summary>Lee las últimas órdenes de Odoo y las resume.</summary>
public sealed class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, IReadOnlyList<SaleSummaryInfo>>
{
    private readonly IPosGateway _gateway;

    /// <summary>Crea el handler.</summary>
    public GetSalesQueryHandler(IPosGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SaleSummaryInfo>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var limit = request.Limit <= 0 ? GetSalesQuery.DefaultLimit : Math.Min(request.Limit, GetSalesQuery.MaxLimit);
        var sales = await _gateway.ListSalesAsync(limit, cancellationToken);
        return sales.Select(ToSummary).ToList();
    }

    private static SaleSummaryInfo ToSummary(SaleInfo s) => new(
        s.OrderId, s.OrderName, s.OperationId, s.Date, s.Total, s.State, s.PaymentMethodName, s.Currency);
}
