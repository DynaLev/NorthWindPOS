using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;

namespace Pos.Application.Tests.TestDoubles;

/// <summary>
/// Doble de prueba del adaptador de Odoo. Cuenta los envíos de venta para probar
/// que nunca se registra dos veces la misma operación.
/// </summary>
internal sealed class FakePosGateway : IPosGateway
{
    public static PosSessionInfo OpenSession() =>
        new(1, "Northwind Caja/00001", 1, "Northwind Caja", "opened", 1, "USD", 2, 1, "Cash", DateTimeOffset.UtcNow);

    public PosSessionInfo? Session { get; set; } = OpenSession();

    /// <summary>Respuestas sucesivas de FindSaleByOperationIdAsync (la última se repite).</summary>
    public Queue<SaleInfo?> FindByOperationResults { get; } = new();

    public Exception? ThrowOnSubmit { get; set; }
    public SaleInfo? SubmitResult { get; set; }
    public Func<long, SaleProduct>? ProductFactory { get; set; }
    public HashSet<long> MissingProducts { get; } = [];

    public int SubmitCalls { get; private set; }
    public int FindByOperationCalls { get; private set; }
    public SaleSubmission? LastSubmission { get; private set; }

    public Task<PosSessionInfo?> FindActiveSessionAsync(CancellationToken cancellationToken) => Task.FromResult(Session);

    public Task<IReadOnlyList<PosSessionInfo>> ListOpenSessionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PosSessionInfo>>(Session is null ? [] : [Session]);

    public Task<PosSessionInfo?> FindSessionAsync(long sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(Session?.Id == sessionId ? Session : null);

    public Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string? search, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProductInfo>>([]);

    public Task<IReadOnlyList<SaleProduct>> GetSaleProductsAsync(IReadOnlyCollection<long> productIds, PosSessionInfo session, IReadOnlyDictionary<long, decimal> quantities, CancellationToken cancellationToken)
    {
        var factory = ProductFactory ?? (id => new SaleProduct(id, $"Prod{id}", true, 10m, 11.5m, [], null, false, "Unidad"));
        IReadOnlyList<SaleProduct> list = productIds.Where(id => !MissingProducts.Contains(id)).Select(factory).ToList();
        return Task.FromResult(list);
    }

    public Task<SaleInfo?> FindSaleByOperationIdAsync(Guid operationId, CancellationToken cancellationToken)
    {
        FindByOperationCalls++;
        var result = FindByOperationResults.Count > 1 ? FindByOperationResults.Dequeue()
            : FindByOperationResults.Count == 1 ? FindByOperationResults.Peek() : null;
        return Task.FromResult(result);
    }

    public Task<SaleInfo?> FindSaleByIdAsync(long orderId, CancellationToken cancellationToken) =>
        Task.FromResult(SubmitResult?.OrderId == orderId ? SubmitResult : null);

    public Task<IReadOnlyList<SaleInfo>> ListSalesAsync(int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SaleInfo>>(SubmitResult is null ? [] : [SubmitResult]);

    public Task<SaleInfo> SubmitSaleAsync(SaleSubmission submission, CancellationToken cancellationToken)
    {
        SubmitCalls++;
        LastSubmission = submission;
        if (ThrowOnSubmit is not null) throw ThrowOnSubmit;
        return Task.FromResult(SubmitResult ?? Sale(submission.OperationId, "paid", submission.TotalWithTax, submission.TotalWithTax));
    }

    public static SaleInfo Sale(Guid operationId, string state, decimal total, decimal paid, long orderId = 123) =>
        new(orderId, $"Northwind Caja/{orderId:00000}", operationId, state, DateTimeOffset.UtcNow, total, 0m, paid, 0m,
            "USD", 1, 1, "Cash", [], [new SalePaymentInfo(1, 1, "Cash", paid, true)]);

    public static PosIntegrationException Failure(PosIntegrationFailure failure) => new(failure, failure.ToString());
}
