using Pos.Application.Common;
using Pos.Application.Models;

namespace Pos.Application.Features.Sales.Common;

/// <summary>
/// RN08: una venta solo se informa como exitosa si Odoo la devuelve pagada
/// (estado paid/done) y con importe pagado mayor o igual al total.
/// </summary>
public static class SaleVerifier
{
    private static readonly string[] PaidStates = ["paid", "done", "invoiced"];

    /// <summary>Indica si la orden cumple RN08.</summary>
    public static bool IsPaid(SaleInfo sale) =>
        PaidStates.Contains(sale.State, StringComparer.OrdinalIgnoreCase)
        && (sale.Total <= 0m || sale.Paid >= sale.Total);

    /// <summary>
    /// Devuelve la orden si está verificada; si no, lanza <see cref="SaleVerificationException"/>.
    /// </summary>
    /// <param name="sale">Orden leída de Odoo.</param>
    /// <param name="operationId">Identificador de operación.</param>
    /// <param name="reconciled">True si la orden se encontró conciliando por uuid.</param>
    public static SaleInfo EnsurePaid(SaleInfo sale, Guid operationId, bool reconciled)
    {
        if (!PaidStates.Contains(sale.State, StringComparer.OrdinalIgnoreCase))
        {
            throw new SaleVerificationException(operationId, sale.OrderId, reconciled
                ? "Se encontró una orden en Odoo con este identificador, pero no está pagada. Verifíquela manualmente."
                : "La orden fue creada en Odoo pero no quedó en estado pagada. Verifíquela en el POS.");
        }

        if (sale.Total > 0m && sale.Paid < sale.Total)
        {
            throw new SaleVerificationException(operationId, sale.OrderId, reconciled
                ? "La orden existe en Odoo pero el pago no está completo. Verifíquela manualmente."
                : "El importe pagado en Odoo es menor al total de la orden. Verifique el pago en efectivo.");
        }

        return sale;
    }
}
