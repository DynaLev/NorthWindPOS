using System.Diagnostics;
using Pos.Application.Abstractions;
using Pos.Application.Common;
using Pos.Application.Models;
using Pos.Domain.Common;
using Pos.Domain.Sales;

namespace Pos.Application.Services;

public sealed class SaleService
{
    private readonly IPosGateway _gateway;
    private readonly IPosSessionService _sessionService;

    public SaleService(IPosGateway gateway, IPosSessionService sessionService)
    {
        _gateway = gateway;
        _sessionService = sessionService;
    }

    public async Task<SaleInfo> CreateAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Lines is null)
        {
            throw new AppException(
                ErrorKind.Validation,
                "SALE_INVALID",
                "La solicitud de venta no contiene líneas.");
        }

        var lines = new List<SaleLine>(request.Lines.Count);
        foreach (var l in request.Lines)
        {
            try
            {
                lines.Add(SaleLine.Create(l.ProductId, l.Quantity));
            }
            catch (DomainValidationException ex)
            {
                throw new AppException(ErrorKind.Validation, ex.Code, ex.Message);
            }
        }

        SaleDraft draft;
        try
        {
            draft = SaleDraft.Create(request.ClientOperationId, lines);
        }
        catch (DomainValidationException ex)
        {
            throw new AppException(ErrorKind.Validation, ex.Code, ex.Message);
        }

        // Si ya existe una orden con este OperationId, devuelve la existente
        // (evita duplicados en reintentos; RN07).
        var existing = await _gateway.FindSaleByOperationIdAsync(draft.OperationId, cancellationToken);
        if (existing is not null)
        {
            return existing with { Replay = true };
        }

        var session = await _sessionService.ResolveForSaleAsync(request.SessionId, cancellationToken);

        var productIds = draft.Lines
            .Select(l => l.ProductId)
            .Distinct()
            .ToArray();
        var saleProducts = await _gateway.GetSaleProductsAsync(productIds, session, cancellationToken);

        var productsById = saleProducts.ToDictionary(p => p.Id);
        var submissionLines = new List<SaleSubmissionLine>(draft.Lines.Count);
        decimal totalWithoutTax = 0m;
        decimal totalWithTax = 0m;

        foreach (var line in draft.Lines)
        {
            if (!productsById.TryGetValue(line.ProductId, out var product))
            {
                throw new AppException(
                    ErrorKind.Validation,
                    "PRODUCT_NOT_VENDIBLE",
                    $"El producto {line.ProductId} no es vendible en este POS o no fue encontrado.");
            }

            if (!product.AvailableInPos)
            {
                throw new AppException(
                    ErrorKind.Validation,
                    "PRODUCT_NOT_VENDIBLE",
                    $"El producto '{product.Name}' no está habilitado para vender en este POS.");
            }

            var subtotalWithoutTax = product.UnitPriceWithoutTax * line.Quantity;
            var subtotalWithTax = product.UnitPriceWithTax * line.Quantity;
            totalWithoutTax += subtotalWithoutTax;
            totalWithTax += subtotalWithTax;

            submissionLines.Add(new SaleSubmissionLine(
                product.Id,
                product.Name,
                line.Quantity,
                product.UnitPriceWithoutTax,
                subtotalWithoutTax,
                subtotalWithTax,
                product.TaxIds));
        }

        var submission = new SaleSubmission(
            draft.OperationId,
            session.Id,
            session.CompanyId,
            totalWithoutTax,
            totalWithTax - totalWithoutTax,
            totalWithTax,
            session.CashPaymentMethodId!.Value,
            submissionLines);

        try
        {
            var result = await _gateway.SubmitSaleAsync(submission, cancellationToken);

            // Verificación mínima: debe estar pagada y con importe pagado >= total (RN08)
            if (!string.Equals(result.State, "paid", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(result.State, "done", StringComparison.OrdinalIgnoreCase))
            {
                throw new SaleVerificationException(
                    draft.OperationId,
                    result.OrderId,
                    "La orden fue creada en Odoo pero no quedó en estado pagada. Verifíquela en el POS.");
            }

            if (result.Paid < result.Total && result.Total > 0m)
            {
                throw new SaleVerificationException(
                    draft.OperationId,
                    result.OrderId,
                    "El importe pagado en Odoo es menor al total de la orden. Verifique el pago en efectivo.");
            }

            return result;
        }
        catch (PosIntegrationException ex) when (ex.Failure == PosIntegrationFailure.Timeout)
        {
            // Ante timeout, conciliamos antes de reintentar (RN07)
            var reconciled = await _gateway.FindSaleByOperationIdAsync(draft.OperationId, cancellationToken);
            if (reconciled is not null)
            {
                if (!string.Equals(reconciled.State, "paid", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(reconciled.State, "done", StringComparison.OrdinalIgnoreCase))
                {
                    throw new SaleVerificationException(draft.OperationId, reconciled.OrderId,
                        "Se encontró una orden en Odoo con este identificador, pero no está pagada. Verifíquela manualmente.");
                }

                if (reconciled.Paid < reconciled.Total && reconciled.Total > 0m)
                {
                    throw new SaleVerificationException(draft.OperationId, reconciled.OrderId,
                        "La orden existe en Odoo pero el pago no está completo. Verifíquela manualmente.");
                }

                return reconciled with { Replay = true };
            }

            throw new SaleResultUncertainException(
                draft.OperationId,
                "Tiempo de espera agotado al registrar la venta en Odoo. No se pudo confirmar si la venta se creó. " +
                "Consulte el historial antes de volver a intentarlo con el mismo identificador.",
                ex);
        }
        catch (PosIntegrationException ex) when (ex.Failure == PosIntegrationFailure.Unavailable)
        {
            var reconciled = await _gateway.FindSaleByOperationIdAsync(draft.OperationId, cancellationToken);
            if (reconciled is not null)
            {
                return reconciled with { Replay = true };
            }

            throw new SaleResultUncertainException(
                draft.OperationId,
                "No se pudo conectar con Odoo al procesar la venta. No se pudo confirmar el resultado. " +
                "Consulte el historial antes de reintentar.",
                ex);
        }
    }

    public async Task<IReadOnlyList<SaleSummaryInfo>> ListAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0) limit = 50;
        if (limit > 200) limit = 200;
        var sales = await _gateway.ListSalesAsync(limit, cancellationToken);
        return sales
            .Select(s => new SaleSummaryInfo(
                s.OrderId,
                s.OrderName,
                s.OperationId,
                s.Date,
                s.Total,
                s.State,
                s.PaymentMethodName,
                s.Currency))
            .ToList();
    }

    public async Task<SaleInfo> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sale = await _gateway.FindSaleByIdAsync(id, cancellationToken);
        if (sale is null)
        {
            throw new AppException(
                ErrorKind.NotFound,
                "SALE_NOT_FOUND",
                "No se encontró la venta solicitada.");
        }
        return sale;
    }

    public async Task<SaleInfo> GetByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var sale = await _gateway.FindSaleByOperationIdAsync(operationId, cancellationToken);
        if (sale is null)
        {
            throw new AppException(
                ErrorKind.NotFound,
                "SALE_NOT_FOUND",
                "No se encontró una venta con ese identificador de operación.");
        }
        return sale;
    }
}
