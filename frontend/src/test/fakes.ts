import type { PosApiPort } from '../application/ports'
import type { PosSession, Product, Sale } from '../domain/models'

export const session: PosSession = {
  id: 1, name: 'Northwind Caja/00001', configId: 1, configName: 'Northwind Caja', state: 'opened',
  currency: 'USD', currencyDecimalPlaces: 2, cashPaymentMethodId: 1, cashPaymentMethodName: 'Cash',
}

export const products: Product[] = [
  { id: 3, name: 'Cafe Norte', code: 'NW-CAFE', barcode: null, price: 2.5, currency: 'USD', availableQuantity: null, tracksInventory: false, unit: 'Unidades' },
  { id: 4, name: 'Sandwich Norte', code: 'NW-SAND', barcode: null, price: 4, currency: 'USD', availableQuantity: 7, tracksInventory: true, unit: 'Unidades' },
]

export function sale(overrides: Partial<Sale> = {}): Sale {
  return {
    orderId: 10, orderName: 'Northwind Caja/0010', operationId: 'op-1', state: 'paid', date: null,
    total: 10.35, tax: 1.35, paid: 10.35, returned: 0, currency: 'USD', sessionId: 1, paymentMethodName: 'Cash',
    lines: [], payments: [], replay: false, ...overrides,
  }
}

export function fakeApi(overrides: Partial<PosApiPort> = {}): PosApiPort {
  return {
    login: async () => ({ accessToken: 't', expiresAtUtc: '', operator: { username: 'cajero', displayName: 'Cajero', role: 'Cashier' } }),
    searchProducts: async () => products,
    getActiveSession: async () => session,
    listOpenSessions: async () => [session],
    createSale: async () => sale(),
    listSales: async () => [],
    getSale: async () => sale(),
    ...overrides,
  }
}
