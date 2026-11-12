// Modelos del dominio del POS web. Sin React ni fetch: solo datos.

export interface Operator {
  username: string
  displayName: string
  role: string
}

export interface AuthToken {
  accessToken: string
  expiresAtUtc: string
  operator: Operator
}

export interface Product {
  id: number
  name: string
  code: string | null
  barcode: string | null
  /** Precio de lista sin impuestos informado por Odoo. */
  price: number
  currency: string
  /** Stock disponible; null si el producto no controla inventario. */
  availableQuantity: number | null
  tracksInventory: boolean
  unit: string
}

export interface PosSession {
  id: number
  name: string
  configId: number
  configName: string
  state: string
  currency: string
  currencyDecimalPlaces?: number
  cashPaymentMethodId: number | null
  cashPaymentMethodName: string | null
}

export interface SaleLine {
  productId: number
  name: string
  quantity: number
  unitPrice: number
  subtotal: number
  subtotalWithTax: number
}

export interface SalePayment {
  id: number
  paymentMethodId: number
  paymentMethodName: string
  amount: number
  isCash: boolean
}

export interface Sale {
  orderId: number
  orderName: string
  operationId: string
  state: string
  date: string | null
  total: number
  tax: number
  paid: number
  returned: number
  currency: string
  sessionId: number
  paymentMethodName: string
  lines: SaleLine[]
  payments: SalePayment[]
  replay: boolean
}

export interface SaleSummary {
  orderId: number
  orderName: string
  operationId: string
  date: string | null
  total: number
  state: string
  paymentMethodName: string
  currency: string
}

/** Línea que el cajero pide; el precio real lo define Odoo al cobrar (RN06). */
export interface SaleLineRequest {
  productId: number
  quantity: number
}
