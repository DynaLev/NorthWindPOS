using Pos.Domain.Common;

namespace Pos.Domain.Sales;

/// <summary>
/// Intención de venta validada antes de llamar a Odoo.
/// El identificador de operación permite reintentos idempotentes (RN07).
/// </summary>
public sealed class SaleDraft
{
    /// <summary>Máximo de líneas por venta.</summary>
    public const int MaxLines = 100;

    private SaleDraft(Guid operationId, IReadOnlyList<SaleLine> lines)
    {
        OperationId = operationId;
        Lines = lines;
    }

    /// <summary>Identificador de operación; se usa como uuid de pos.order (RN07).</summary>
    public Guid OperationId { get; }

    /// <summary>Líneas validadas.</summary>
    public IReadOnlyList<SaleLine> Lines { get; }

    /// <summary>Crea la venta o lanza <see cref="DomainValidationException"/> si no tiene líneas (RN01).</summary>
    /// <param name="operationId">Identificador del cliente; si falta se genera uno nuevo.</param>
    /// <param name="lines">Líneas ya validadas.</param>
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
