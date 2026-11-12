using Pos.Application.Models;
using Pos.Application.Tests.TestDoubles;

namespace Pos.Application.Tests;

/// <summary>RN05 y RN06: el envío usa precios de Odoo, el método cash de la sesión y redondeo de la moneda.</summary>
public sealed class SaleSubmissionTests
{
    [Fact]
    public async Task Submission_UsesOdooPrices_CashMethod_AndRoundsToCurrency()
    {
        var gateway = new FakePosGateway
        {
            ProductFactory = id => id == 3
                ? new SaleProduct(3, "Cafe Norte", true, 2.5m, 2.875m, [1], null, false, "Unidad")
                : new SaleProduct(4, "Sandwich Norte", true, 4m, 4.6m, [1], null, false, "Unidad")
        };
        var op = Guid.NewGuid();

        var result = await HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None);

        var submission = gateway.LastSubmission!;
        Assert.False(result.Replay);
        Assert.Equal(op, submission.OperationId);
        Assert.Equal(1, submission.CashPaymentMethodId);
        Assert.Equal(1, submission.SessionId);
        Assert.Equal(5.75m, submission.Lines[0].SubtotalWithTax);   // 2.875 × 2
        Assert.Equal(4.60m, submission.Lines[1].SubtotalWithTax);
        Assert.Equal(9.00m, submission.TotalWithoutTax);
        Assert.Equal(10.35m, submission.TotalWithTax);
        Assert.Equal(1.35m, submission.TotalTax);
    }

    [Fact]
    public async Task Submission_RoundsHalfUp()
    {
        var gateway = new FakePosGateway
        {
            ProductFactory = id => new SaleProduct(id, "X", true, 0.335m, 0.385m, [], null, false, "Unidad")
        };
        var command = new Pos.Application.Features.Sales.Commands.CreateSale.CreateSaleCommand(
            Guid.NewGuid(), null, [new SaleLineRequest(3, 1)]);

        await HandlerFactory.CreateSale(gateway).Handle(command, CancellationToken.None);

        Assert.Equal(0.34m, gateway.LastSubmission!.Lines[0].SubtotalWithoutTax);
        Assert.Equal(0.39m, gateway.LastSubmission.Lines[0].SubtotalWithTax);
    }
}
