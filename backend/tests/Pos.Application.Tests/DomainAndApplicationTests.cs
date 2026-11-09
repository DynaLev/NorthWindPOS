using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;
using Pos.Application.Services;
using Pos.Domain.Sales;

namespace Pos.Application.Tests;

public sealed class DomainAndApplicationTests
{
    private class FakeGateway : IPosGateway
    {
        public PosSessionInfo? Session { get; set; }
        public SaleInfo? FoundByOp { get; set; }
        public SaleInfo? Submitted { get; set; }
        public PosIntegrationException? ThrowOnSubmit { get; set; }

        public Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken cancellationToken)
            => Task.FromResult(Session);

        public Task<PosSessionInfo?> FindSessionAsync(long sessionId, CancellationToken cancellationToken)
            => Task.FromResult(Session);

        public Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string? search, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<ProductInfo>)Array.Empty<ProductInfo>());

        public Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(IReadOnlyCollection<long> productIds, PosSessionInfo session, CancellationToken cancellationToken)
        {
            var list = new List<SaleProduct>();
            foreach (var id in productIds)
            {
                list.Add(new SaleProduct(id, $"Prod{id}", true, 10m, 11.5m, Array.Empty<long>(), 100m, false, "Unit"));
            }
            return Task.FromResult((IReadOnlyList<SaleProduct>)list);
        }

        public Task<SaleInfo?> FindSaleByOperationIdAsync(Guid operationId, CancellationToken cancellationToken)
            => Task.FromResult(FoundByOp);

        public Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken cancellationToken)
            => Task.FromResult(Submitted);

        public Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<SaleInfo>)Array.Empty<SaleInfo>());

        public Task<SaleInfo> SubmitSaleAsync(SaleSubmission submission, CancellationToken cancellationToken)
        {
            if (ThrowOnSubmit is not null) throw ThrowOnSubmit;
            return Task.FromResult(Submitted ?? new SaleInfo(1, "Order 00001", submission.OperationId, "paid", DateTimeOffset.UtcNow, submission.TotalWithTax, submission.TotalTax, submission.TotalWithTax, 0m, "USD", submission.SessionId, 1, "Cash", Array.Empty<SaleLineInfo>(), Array.Empty<SalePaymentInfo>()));
        }
    }

    private sealed class TestSessionService : IPosSessionService
    {
        public PosSessionInfo Session { get; set; } = new(1, "Session/1", 10, "POS1", "opened", 1, "USD", 2, 5, "Cash", DateTimeOffset.UtcNow);
        public Task<PosSessionInfo> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult(Session);
        public Task<PosSessionInfo> ResolveForSaleAsync(long? sessionId, CancellationToken cancellationToken) => Task.FromResult(Session);
    }

    [Fact]
    public void P01_RejectEmptySale()
    {
        var draft = Assert.Throws<Pos.Domain.Common.DomainValidationException>(() => SaleDraft.Create(Guid.NewGuid(), Array.Empty<SaleLine>()));
        Assert.Equal("SALE_EMPTY", draft.Code);
    }

    [Fact]
    public void P02_RejectZeroOrNegativeQuantity()
    {
        var ex1 = Assert.Throws<Pos.Domain.Common.DomainValidationException>(() => SaleLine.Create(1, 0));
        Assert.Equal("QUANTITY_INVALID", ex1.Code);
        var ex2 = Assert.Throws<Pos.Domain.Common.DomainValidationException>(() => SaleLine.Create(1, -1));
        Assert.Equal("QUANTITY_INVALID", ex2.Code);
    }

    [Fact]
    public async Task P03_RejectInvalidOrClosedSession()
    {
        var gateway = new FakeGateway { Session = new PosSessionInfo(1, "S1", 1, "P1", "closed", 1, "USD", 2, 5, "Cash", null) };
        var svc = new PosSessionService(gateway);
        var ex = await Assert.ThrowsAsync<AppException>(() => svc.ResolveForSaleAsync(null, CancellationToken.None));
        Assert.Equal("POS_SESSION_NOT_OPEN", ex.Code);
    }

    [Fact]
    public async Task P04_RejectNonVendibleProduct()
    {
        var gateway = new FakeGateway
        {
            Session = new PosSessionInfo(1, "S1", 10, "P1", "opened", 1, "USD", 2, 5, "Cash", null)
        };
        // Sobreescribir para producto no vendible
        var gateway2 = new GatewayNoVendible(gateway.Session);
        var svc = new SaleService(gateway2, new TestSessionService());
        var req = new CreateSaleRequest(Guid.NewGuid(), null, new[] { new SaleLineRequest(99, 1) });
        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(req, CancellationToken.None));
        Assert.Equal("PRODUCT_NOT_VENDIBLE", ex.Code);
    }

    private sealed class GatewayNoVendible : IPosGateway
    {
        private readonly PosSessionInfo _session;
        public GatewayNoVendible(PosSessionInfo s) { _session = s; }
        public Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken cancellationToken) => Task.FromResult<PosSessionInfo?>(_session);
        public Task<PosSessionInfo?> FindSessionAsync(long sessionId, CancellationToken cancellationToken) => Task.FromResult<PosSessionInfo?>(_session);
        public Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string? search, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<ProductInfo>)Array.Empty<ProductInfo>());
        public Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(IReadOnlyCollection<long> productIds, PosSessionInfo session, CancellationToken cancellationToken)
            => Task.FromResult((IReadOnlyList<SaleProduct>)new[] { new SaleProduct(99, "X", false, 10m, 11.5m, Array.Empty<long>(), 1m, false, "Unit") });
        public Task<SaleInfo?> FindSaleByOperationIdAsync(Guid operationId, CancellationToken cancellationToken) => Task.FromResult<SaleInfo?>(null);
        public Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken cancellationToken) => Task.FromResult<SaleInfo?>(null);
        public Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<SaleInfo>)Array.Empty<SaleInfo>());
        public Task<SaleInfo> SubmitSaleAsync(SaleSubmission submission, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    [Fact]
    public async Task P05_TimeoutDoesNotDeclareSuccess()
    {
        var gateway = new FakeGateway
        {
            Session = new PosSessionInfo(1, "S1", 10, "P1", "opened", 1, "USD", 2, 5, "Cash", null),
            ThrowOnSubmit = new PosIntegrationException(PosIntegrationFailure.Timeout, "timeout"),
            FoundByOp = null
        };
        var svc = new SaleService(gateway, new TestSessionService());
        var req = new CreateSaleRequest(Guid.NewGuid(), null, new[] { new SaleLineRequest(1, 1) });
        var ex = await Assert.ThrowsAsync<SaleResultUncertainException>(() => svc.CreateAsync(req, CancellationToken.None));
        Assert.Equal("SALE_RESULT_UNCERTAIN", ex.Code);
    }

    [Fact]
    public async Task P06_RetryDoesNotDuplicateWhenFound()
    {
        var op = Guid.NewGuid();
        var found = new SaleInfo(123, "Order 000123", op, "paid", DateTimeOffset.UtcNow, 11.5m, 1.5m, 11.5m, 0m, "USD", 1, 10, "Cash", Array.Empty<SaleLineInfo>(), Array.Empty<SalePaymentInfo>());
        var gateway = new FakeGateway
        {
            Session = new PosSessionInfo(1, "S1", 10, "P1", "opened", 1, "USD", 2, 5, "Cash", null),
            FoundByOp = found
        };
        var svc = new SaleService(gateway, new TestSessionService());
        var req = new CreateSaleRequest(op, null, new[] { new SaleLineRequest(1, 1) });
        var result = await svc.CreateAsync(req, CancellationToken.None);
        Assert.True(result.Replay);
        Assert.Equal(123, result.OrderId);
    }
}
