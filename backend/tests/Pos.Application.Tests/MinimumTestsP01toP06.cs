using Pos.Application.Common;
using Pos.Application.Common.Exceptions;
using Pos.Application.Features.Sales.Commands.CreateSale;
using Pos.Application.Models;
using Pos.Application.Services;
using Pos.Application.Tests.TestDoubles;
using Pos.Domain.Common;
using Pos.Domain.Sales;

namespace Pos.Application.Tests;

/// <summary>Pruebas mínimas P01–P06 del documento del proyecto.</summary>
public sealed class MinimumTestsP01toP06
{
    [Fact]
    public void P01_RejectEmptySale_InDomain()
    {
        var ex = Assert.Throws<DomainValidationException>(() => SaleDraft.Create(Guid.NewGuid(), []));
        Assert.Equal("SALE_EMPTY", ex.Code);
    }

    [Fact]
    public async Task P01_RejectEmptySale_WithoutCallingOdoo()
    {
        var gateway = new FakePosGateway();
        var handler = HandlerFactory.CreateSale(gateway);
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            handler.Handle(new CreateSaleCommand(Guid.NewGuid(), null, []), CancellationToken.None));
        Assert.Equal("SALE_EMPTY", ex.Code);
        Assert.Equal(0, gateway.FindByOperationCalls);
        Assert.Equal(0, gateway.SubmitCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void P02_RejectZeroOrNegativeQuantity(decimal quantity)
    {
        var ex = Assert.Throws<DomainValidationException>(() => SaleLine.Create(1, quantity));
        Assert.Equal("QUANTITY_INVALID", ex.Code);
    }

    [Fact]
    public async Task P03_RejectClosedSession()
    {
        var gateway = new FakePosGateway { Session = FakePosGateway.OpenSession() with { State = "closed" } };
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            new PosSessionService(gateway).ResolveForSaleAsync(null, CancellationToken.None));
        Assert.Equal("POS_SESSION_NOT_OPEN", ex.Code);
    }

    [Fact]
    public async Task P03_RejectMissingSession()
    {
        var gateway = new FakePosGateway { Session = null };
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            new PosSessionService(gateway).ResolveForSaleAsync(null, CancellationToken.None));
        Assert.Equal("POS_SESSION_NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task P03_RejectSessionWithoutCashMethod()
    {
        var gateway = new FakePosGateway { Session = FakePosGateway.OpenSession() with { CashPaymentMethodId = null } };
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            new PosSessionService(gateway).GetActiveAsync(CancellationToken.None));
        Assert.Equal("POS_CASH_METHOD_MISSING", ex.Code);
    }

    [Fact]
    public async Task P04_RejectNonVendibleProduct_WithoutSubmitting()
    {
        var gateway = new FakePosGateway
        {
            ProductFactory = id => new SaleProduct(id, "Service Fee", false, 10m, 11.5m, [], null, false, "Unidad")
        };
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(), CancellationToken.None));
        Assert.Equal("PRODUCT_NOT_VENDIBLE", ex.Code);
        Assert.Equal(0, gateway.SubmitCalls);
    }

    [Fact]
    public async Task P04_RejectUnknownProduct_WithoutSubmitting()
    {
        var gateway = new FakePosGateway();
        gateway.MissingProducts.Add(4);
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(), CancellationToken.None));
        Assert.Equal("PRODUCT_NOT_VENDIBLE", ex.Code);
        Assert.Equal(0, gateway.SubmitCalls);
    }

    [Theory]
    [InlineData(PosIntegrationFailure.Timeout)]
    [InlineData(PosIntegrationFailure.Unavailable)]
    public async Task P05_OdooFailureWithoutOrder_IsUncertain_NotSuccess(PosIntegrationFailure failure)
    {
        var gateway = new FakePosGateway { ThrowOnSubmit = FakePosGateway.Failure(failure) };
        var ex = await Assert.ThrowsAsync<SaleResultUncertainException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(), CancellationToken.None));
        Assert.Equal("SALE_RESULT_UNCERTAIN", ex.Code);
        Assert.Equal(1, gateway.SubmitCalls);
    }

    [Fact]
    public async Task P05_OrderNotPaid_IsNotReportedAsSuccess()
    {
        var gateway = new FakePosGateway();
        gateway.SubmitResult = FakePosGateway.Sale(Guid.NewGuid(), "draft", 9m, 0m);
        var ex = await Assert.ThrowsAsync<SaleVerificationException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(), CancellationToken.None));
        Assert.Equal("SALE_NOT_VERIFIED", ex.Code);
    }

    [Fact]
    public async Task P05_PaidLessThanTotal_IsNotReportedAsSuccess()
    {
        var gateway = new FakePosGateway();
        gateway.SubmitResult = FakePosGateway.Sale(Guid.NewGuid(), "paid", 9m, 5m);
        await Assert.ThrowsAsync<SaleVerificationException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(), CancellationToken.None));
    }

    [Theory]
    [InlineData(PosIntegrationFailure.Authentication)]
    [InlineData(PosIntegrationFailure.Permission)]
    [InlineData(PosIntegrationFailure.Rejected)]
    public async Task P05_DefinitiveOdooErrors_PropagateWithoutSuccess(PosIntegrationFailure failure)
    {
        var gateway = new FakePosGateway { ThrowOnSubmit = FakePosGateway.Failure(failure) };
        var ex = await Assert.ThrowsAsync<PosIntegrationException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(), CancellationToken.None));
        Assert.Equal(failure, ex.Failure);
    }

    [Fact]
    public async Task P06_RetryWhenOrderAlreadyExists_DoesNotSubmitAgain()
    {
        var op = Guid.NewGuid();
        var gateway = new FakePosGateway();
        gateway.FindByOperationResults.Enqueue(FakePosGateway.Sale(op, "paid", 9m, 9m));

        var result = await HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None);

        Assert.True(result.Replay);
        Assert.Equal(123, result.OrderId);
        Assert.Equal(0, gateway.SubmitCalls);
    }

    [Fact]
    public async Task P06_TimeoutThenReconcile_SubmitsOnce_ReplayAfterVerification()
    {
        var op = Guid.NewGuid();
        var gateway = new FakePosGateway { ThrowOnSubmit = FakePosGateway.Failure(PosIntegrationFailure.Timeout) };
        gateway.FindByOperationResults.Enqueue(null);                                   // antes de enviar: no existe
        gateway.FindByOperationResults.Enqueue(FakePosGateway.Sale(op, "paid", 9m, 9m)); // tras el timeout: sí existe

        var result = await HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None);

        Assert.Equal(1, gateway.SubmitCalls);
        Assert.True(result.Replay);
        Assert.Equal(op, result.OperationId);
    }

    [Fact]
    public async Task P06_TimeoutThenReconcile_UnpaidOrder_IsNotSuccess()
    {
        var op = Guid.NewGuid();
        var gateway = new FakePosGateway { ThrowOnSubmit = FakePosGateway.Failure(PosIntegrationFailure.Timeout) };
        gateway.FindByOperationResults.Enqueue(null);
        gateway.FindByOperationResults.Enqueue(FakePosGateway.Sale(op, "draft", 9m, 0m));

        await Assert.ThrowsAsync<SaleVerificationException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None));
        Assert.Equal(1, gateway.SubmitCalls);
    }

    [Theory]
    [InlineData("draft", 9, 0)]
    [InlineData("paid", 9, 4)]
    public async Task RN08_UnavailableThenReconcile_StillVerifiesStateAndPayment(string state, decimal total, decimal paid)
    {
        var op = Guid.NewGuid();
        var gateway = new FakePosGateway { ThrowOnSubmit = FakePosGateway.Failure(PosIntegrationFailure.Unavailable) };
        gateway.FindByOperationResults.Enqueue(null);
        gateway.FindByOperationResults.Enqueue(FakePosGateway.Sale(op, state, total, paid));

        await Assert.ThrowsAsync<SaleVerificationException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None));
    }

    [Fact]
    public async Task RN08_UnavailableThenReconcile_PaidOrder_IsReplay()
    {
        var op = Guid.NewGuid();
        var gateway = new FakePosGateway { ThrowOnSubmit = FakePosGateway.Failure(PosIntegrationFailure.Unavailable) };
        gateway.FindByOperationResults.Enqueue(null);
        gateway.FindByOperationResults.Enqueue(FakePosGateway.Sale(op, "paid", 9m, 9m));

        var result = await HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None);
        Assert.True(result.Replay);
        Assert.Equal(1, gateway.SubmitCalls);
    }

    [Fact]
    public async Task RN08_ExistingUnpaidOrder_IsNotReturnedAsSuccess()
    {
        var op = Guid.NewGuid();
        var gateway = new FakePosGateway();
        gateway.FindByOperationResults.Enqueue(FakePosGateway.Sale(op, "draft", 9m, 0m));

        await Assert.ThrowsAsync<SaleVerificationException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None));
        Assert.Equal(0, gateway.SubmitCalls);
    }

    [Fact]
    public async Task RN04_ExplicitSessionNotFound_IsRejected()
    {
        var gateway = new FakePosGateway();
        var command = new CreateSaleCommand(Guid.NewGuid(), 99, [new SaleLineRequest(3, 1)]);
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            HandlerFactory.CreateSale(gateway).Handle(command, CancellationToken.None));
        Assert.Equal("POS_SESSION_NOT_FOUND", ex.Code);
        Assert.Equal(0, gateway.SubmitCalls);
    }
}
