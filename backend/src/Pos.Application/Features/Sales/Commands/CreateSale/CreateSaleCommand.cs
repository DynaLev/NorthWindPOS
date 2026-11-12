using Pos.Application.Common.Mediator;
using Pos.Application.Models;

namespace Pos.Application.Features.Sales.Commands.CreateSale;

/// <summary>
/// Registra una venta en efectivo en el POS real de Odoo (RF05, RF06).
/// </summary>
/// <param name="ClientOperationId">Identificador único de la operación; se guarda como uuid de pos.order (RN07).</param>
/// <param name="SessionId">Sesión POS elegida; si falta se usa la sesión activa.</param>
/// <param name="Lines">Productos y cantidades. Los precios los define Odoo (RN06).</param>
public sealed record CreateSaleCommand(
    Guid? ClientOperationId,
    long? SessionId,
    IReadOnlyList<SaleLineRequest>? Lines) : IRequest<SaleInfo>;
