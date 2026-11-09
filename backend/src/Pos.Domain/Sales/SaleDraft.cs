using Pos.Domain.Common;

namespace Pos.Domain.Sales;

/// <summary>
/// Intención de venta validada antes de llamar a Odoo.
/// El identificador de operación permite reintentos idempotentes (RN07).
/// </summary>
public sealed class SaleDraft
{
    public const int MaxLines = 100;

    private SaleDraft(Guid operationId, IReadOnlyList<SaleLine> lines)
    {
        OperationId = operationId;
        Lines = lines;
    }

    public Guid OperationId { get; }

    public IReadOnlyList<SaleLine> Lines { get; }

    public static SaleDraft Create(Guid? operationId, IEnumerable<SaleLine>? lines)
    {
        var materializedLines = lines?.ToList() ?? [];

        if (materializedLines.Count == 0)
        {
            throw new DomainValidationException(
                "SALE_EMPTY",
                "La venta debe contener al menos una línea de producto.");
        }

        if (materializedLines.Count > MaxLines)
        {
            throw new DomainValidationException(
                "SALE_TOO_LARGE",
                $"La venta no puede contener más de {MaxLines} líneas.");
        }

        var normalizedOperationId = operationId is null || operationId.Value == Guid.Empty
            ? Guid.NewGuid()
            : operationId.Value;

        return new SaleDraft(normalizedOperationId, materializedLines);
    }
}
