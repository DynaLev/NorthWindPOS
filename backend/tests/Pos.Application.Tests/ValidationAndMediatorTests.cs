using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pos.Application.Abstractions;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Mediator;
using Pos.Application.Features.Auth.Commands.Login;
using Pos.Application.Features.Sales.Commands.CreateSale;
using Pos.Application.Features.Sales.Queries.GetSaleById;
using Pos.Application.Models;
using Pos.Application.Tests.TestDoubles;

namespace Pos.Application.Tests;

/// <summary>Validators de FluentValidation y pipeline del SimpleMediator.</summary>
public sealed class ValidationAndMediatorTests
{
    private static ServiceProvider BuildProvider(FakePosGateway gateway)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IPosGateway>(gateway);
        services.AddApplicationServices();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void CreateSaleValidator_RejectsEmptyNullAndInvalidLines()
    {
        var validator = new CreateSaleCommandValidator();
        Assert.Contains(validator.Validate(new CreateSaleCommand(null, null, null)).Errors, e => e.ErrorCode == "SALE_EMPTY");
        Assert.Contains(validator.Validate(new CreateSaleCommand(null, null, [])).Errors, e => e.ErrorCode == "SALE_EMPTY");

        var invalid = validator.Validate(new CreateSaleCommand(null, 0, [new SaleLineRequest(0, 0), new SaleLineRequest(5, -2)]));
        Assert.Contains(invalid.Errors, e => e.ErrorCode == "PRODUCT_ID_INVALID");
        Assert.Equal(2, invalid.Errors.Count(e => e.ErrorCode == "QUANTITY_INVALID"));
        Assert.Contains(invalid.Errors, e => e.ErrorCode == "POS_SESSION_INVALID");
    }

    [Fact]
    public void CreateSaleValidator_AcceptsValidSale()
    {
        Assert.True(new CreateSaleCommandValidator().Validate(HandlerFactory.TwoProductSale()).IsValid);
    }

    [Fact]
    public void LoginValidator_RequiresUserAndPassword()
    {
        var result = new LoginCommandValidator().Validate(new LoginCommand(" ", null));
        Assert.Contains(result.Errors, e => e.ErrorCode == "AUTH_USERNAME_REQUIRED");
        Assert.Contains(result.Errors, e => e.ErrorCode == "AUTH_PASSWORD_REQUIRED");
    }

    [Fact]
    public async Task Mediator_RunsValidatorsBeforeHandler_AndNeverCallsOdoo()
    {
        var gateway = new FakePosGateway();
        using var provider = BuildProvider(gateway);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<CustomValidationException>(() =>
            mediator.Send(new CreateSaleCommand(Guid.NewGuid(), null, [new SaleLineRequest(3, 0)])));

        Assert.Equal("QUANTITY_INVALID", ex.Code);
        Assert.NotEmpty(ex.ValidationErrors);
        Assert.Equal(0, gateway.FindByOperationCalls);
        Assert.Equal(0, gateway.SubmitCalls);
    }

    [Fact]
    public async Task Mediator_DispatchesToHandler()
    {
        var gateway = new FakePosGateway();
        using var provider = BuildProvider(gateway);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var sale = await mediator.Send(HandlerFactory.TwoProductSale());
        Assert.Equal("paid", sale.State);

        await Assert.ThrowsAsync<NotFoundException>(() => mediator.Send(new GetSaleByIdQuery(999)));
    }

    [Fact]
    public void RegisterApplicationServices_RegistersHandlersAndValidators()
    {
        using var provider = BuildProvider(new FakePosGateway());
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IRequestHandler<CreateSaleCommand, SaleInfo>>());
        Assert.NotNull(scope.ServiceProvider.GetService<IValidator<CreateSaleCommand>>());
        Assert.NotNull(scope.ServiceProvider.GetService<IValidator<LoginCommand>>());
    }

    [Fact]
    public async Task ConcurrentRequestsWithSameOperation_SubmitOnlyOnce()
    {
        var op = Guid.NewGuid();
        var gateway = new ConcurrencyGateway(op);
        var handler = new CreateSaleCommandHandler(gateway, new Services.PosSessionService(gateway),
            new Common.SaleOperationLock(), NullLogger<CreateSaleCommandHandler>.Instance);

        var results = await Task.WhenAll(
            handler.Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None),
            handler.Handle(HandlerFactory.TwoProductSale(op), CancellationToken.None));

        Assert.Equal(1, gateway.SubmitCalls);
        Assert.Single(results, r => r.Replay);
    }

    /// <summary>Simula que Odoo guarda la orden al enviarla, con algo de latencia.</summary>
    private sealed class ConcurrencyGateway(Guid op) : IPosGateway
    {
        private readonly FakePosGateway _inner = new();
        private SaleInfo? _stored;
        public int SubmitCalls { get; private set; }

        public Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken ct) => _inner.FindActiveSessionAsync(ct);
        public Task<IReadOnlyList<PosSessionInfo>> ListOpenSessionsAsync(CancellationToken ct) => _inner.ListOpenSessionsAsync(ct);
        public Task<PosSessionInfo?> FindSessionAsync(long id, CancellationToken ct) => _inner.FindSessionAsync(id, ct);
        public Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string? s, CancellationToken ct) => _inner.SearchProductsAsync(s, ct);
        public Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(IReadOnlyCollection<long> ids, PosSessionInfo session, IReadOnlyDictionary<long, decimal> quantities, CancellationToken ct)
            => _inner.GetSaleProductsAsync(ids, session, quantities, ct);
        public Task<SaleInfo?> FindSaleByOperationIdAsync(Guid operationId, CancellationToken ct) => Task.FromResult(_stored);
        public Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken ct) => Task.FromResult(_stored);
        public Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken ct) => _inner.ListSalesAsync(limit, ct);

        public async Task<SaleInfo> SubmitSaleAsync(SaleSubmission submission, CancellationToken ct)
        {
            SubmitCalls++;
            await Task.Delay(50, ct);
            _stored = FakePosGateway.Sale(op, "paid", submission.TotalWithTax, submission.TotalWithTax);
            return _stored;
        }
    }
}
