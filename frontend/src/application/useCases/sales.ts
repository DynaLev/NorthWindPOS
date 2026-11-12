import type { Sale, SaleSummary } from '../../domain/models'
import { ApiError } from '../errors'
import type { PosApiPort } from '../ports'

/** Historial de ventas leído de Odoo a través de la API (RF07). */
export function loadSales(api: PosApiPort): Promise<SaleSummary[]> {
  return api.listSales()
}

export function loadSaleDetail(api: PosApiPort, id: number): Promise<Sale> {
  if (!Number.isInteger(id) || id <= 0) {
    return Promise.reject(new ApiError(400, 'SALE_ID_INVALID', 'Identificador de venta inválido.'))
  }
  return api.getSale(id)
}
