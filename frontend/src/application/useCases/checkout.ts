import { isEmpty, toSaleLines, type Cart } from '../../domain/cart'
import { formatMoney } from '../../domain/money'
import { describeSaleState } from '../../domain/saleStatus'
import type { Sale } from '../../domain/models'
import { describeError, isUncertain } from '../errors'
import type { PosApiPort } from '../ports'

export type CheckoutResult =
  | { kind: 'success'; sale: Sale; message: string }
  | { kind: 'replay'; sale: Sale; message: string }
  | { kind: 'uncertain'; message: string }
  | { kind: 'error'; message: string }

export interface CheckoutInput {
  cart: Cart
  operationId: string
  sessionId?: number
}

/**
 * Cobro en efectivo. Solo devuelve "success" cuando la API respondió 200, es decir, cuando
 * Odoo confirmó la orden pagada (RN08). Ante resultado incierto el llamador debe conservar
 * el mismo operationId para reintentar sin duplicar (RN07).
 */
export async function checkout(api: PosApiPort, input: CheckoutInput): Promise<CheckoutResult> {
  if (isEmpty(input.cart)) {
    return { kind: 'error', message: 'Agregue al menos un producto antes de cobrar.' }
  }
  try {
    const sale = await api.createSale({
      clientOperationId: input.operationId,
      sessionId: input.sessionId,
      lines: toSaleLines(input.cart),
    })
    return sale.replay
      ? { kind: 'replay', sale, message: `Esa operación ya estaba registrada como ${sale.orderName}. No se duplicó la venta.` }
      : { kind: 'success', sale, message: successMessage(sale) }
  } catch (error) {
    if (isUncertain(error)) {
      return {
        kind: 'uncertain',
        message: `${describeError(error, 'No se pudo confirmar la venta.')} Puede volver a cobrar: se reutilizará el mismo identificador.`,
      }
    }
    return { kind: 'error', message: describeError(error, 'No se pudo cobrar.') }
  }
}

function successMessage(sale: Sale): string {
  return `Venta ${sale.orderName} registrada en Odoo. Estado: ${describeSaleState(sale.state)}. ` +
    `Total cobrado (con impuestos): ${formatMoney(sale.total, sale.currency)}.`
}
