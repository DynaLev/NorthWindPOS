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
  price: number
  currency: string
  unit: string
}

export interface PosSession {
  id: number
  name: string
  configId: number
  configName: string
  state: string
  currency: string
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

export interface CartLine {
  productId: number
  name: string
  quantity: number
  unitPrice: number
}
