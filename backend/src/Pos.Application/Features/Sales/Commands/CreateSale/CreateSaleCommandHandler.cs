using Microsoft.Extensions.Logging;
using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Common.Mediator;
using Pos.Application.Features.Sales.Common;
using Pos.Application.Models;
using Pos.Domain.Common;
using Pos.Domain.Sales;

namespace Pos.Application.Features.Sales.Commands.CreateSale;

/// <summary>
/// Caso de uso de cobro en efectivo. Idempotente por uuid (RN07): primero concilia,
/// después envía con pos.order.sync_from_ui y solo informa éxito tras verificar en Odoo (RN08).
/// Ante timeout o caída de Odoo nunca reenvía a ciegas: busca la orden por uuid y, si no la
/// encuentra, responde SALE_RESULT_UNCERTAIN para que el cliente reintente con el mismo id.
/// </summary>
public sealed class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, SaleInfo>
{
    private readonly IPosGateway _gateway;
    private readonly IPosSessionService _sessions;
    private readonly SaleOperationLock _operationLock;
    private readonly ILogger<CreateSaleCommandHandler> _logger;

    /// <summary>Crea el handler.</summary>
    public CreateSaleCommandHandler(
        IPosGateway gateway,
        IPosSessionService sessions,
        SaleOperationLock operationLock,
        ILogger<CreateSaleCommandHandler> logger)
    {
        _gateway = gateway;
        _sessions = sessions;
        _operationLock = operationLock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SaleInfo> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var draft = CreateDraft(request);
        using var turn = await _operationLock.AcquireAsync(draft.OperationId, cancellationToken);

        var existing = await _gateway.FindSaleByOperationIdAsync(draft.OperationId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Venta {OperationId} ya existía en Odoo como orden {OrderId}; no se reenvía.",
                draft.OperationId, existing.OrderId);
            return SaleVerifier.EnsurePaid(existing, draft.OperationId, reconciled: true) with { Replay = true };
        }

        var session = await _sessions.ResolveForSaleAsync(request.SessionId, cancellationToken);
        var productIds = draft.Lines.Select(l => l.ProductId).Distinct().ToArray();
        var quantities = draft.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var products = await _gateway.GetSaleProductsAsync(productIds, session, quantities, cancellationToken);
        var submission = SaleSubmissionBuilder.Build(draft, session, products);

        return await SubmitAndVerifyAsync(submission, cancellationToken);
    }

    /// <summary>
    /// Segunda barrera de RN01/RN02 en el dominio (el validator ya las comprobó).
    /// </summary>
    private static SaleDraft CreateDraft(CreateSaleCommand request)
    {
        try
        {
            var lines = (request.Lines ?? []).Select(l => SaleLine.Create(l.ProductId, l.Quantity));
            return SaleDraft.Create(request.ClientOperationId, lines);
        }
        catch (DomainValidationException ex)
        {
            throw new AppException(ErrorKind.Validation, ex.Code, ex.Message);
        }
    }

    private async Task<SaleInfo> SubmitAndVerifyAsync(SaleSubmission submission, CancellationToken cancellationToken)
    {
        SaleInfo result;
        try
        {
            result = await _gateway.SubmitSaleAsync(submission, cancellationToken);
        }
        catch (PosIntegrationException ex) when (ex.IsTransient)
        {
            _logger.LogWarning("Falla {Failure} al enviar la venta {OperationId}; se concilia por uuid.",
                ex.Failure, submission.OperationId);
            return await ReconcileAsync(submission.OperationId, ex, cancellationToken);
        }

        var verified = SaleVerifier.EnsurePaid(result, submission.OperationId, reconciled: false);
        _logger.LogInformation("Venta {OperationId} registrada y verificada en Odoo como orden {OrderId}.",
            submission.OperationId, verified.OrderId);
        return verified;
    }

    /// <summary>
    /// Rama Timeout y rama Unavailable: misma conciliación y misma verificación (RN07, RN08).
    /// </summary>
    private async Task<SaleInfo> ReconcileAsync(Guid operationId, PosIntegrationException failure, CancellationToken cancellationToken)
    {
        SaleInfo? reconciled;
        try
        {
            reconciled = await _gateway.FindSaleByOperationIdAsync(operationId, cancellationToken);
        }
        catch (PosIntegrationException readFailure) when (readFailure.IsTransient)
        {
            throw Uncertain(operationId, failure);
        }

        if (reconciled is null)
        {
            throw Uncertain(operationId, failure);
        }

        return SaleVerifier.EnsurePaid(reconciled, operationId, reconciled: true) with { Replay = true };
    }

    private static SaleResultUncertainException Uncertain(Guid operationId, PosIntegrationException failure)
    {
        var message = failure.Failure == PosIntegrationFailure.Timeout
            ? "Tiempo de espera agotado al registrar la venta en Odoo. No se pudo confirmar si la venta se creó. " +
              "Consulte el historial antes de volver a intentarlo con el mismo identificador."
            : "No se pudo conectar con Odoo al procesar la venta. No se pudo confirmar el resultado. " +
              "Consulte el historial antes de reintentar.";
        return new SaleResultUncertainException(operationId, message, failure);
    }
}
