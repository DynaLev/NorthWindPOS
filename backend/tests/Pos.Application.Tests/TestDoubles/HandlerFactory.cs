using Microsoft.Extensions.Logging.Abstractions;
using Pos.Application.Common;
using Pos.Application.Features.Sales.Commands.CreateSale;
using Pos.Application.Models;
using Pos.Application.Services;

namespace Pos.Application.Tests.TestDoubles;

internal static class HandlerFactory
{
    public static CreateSaleCommandHandler CreateSale(FakePosGateway gateway) =>
        new(gateway, new PosSessionService(gateway), new SaleOperationLock(), NullLogger<CreateSaleCommandHandler>.Instance);

    public static CreateSaleCommand TwoProductSale(Guid? operationId = null) =>
        new(operationId ?? Guid.NewGuid(), null, [new SaleLineRequest(3, 2), new SaleLineRequest(4, 1)]);
}
