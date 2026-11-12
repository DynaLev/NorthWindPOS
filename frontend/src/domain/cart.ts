// Carrito del cajero: funciones puras e inmutables (RF03). El total es provisional y
// sin impuestos; el importe definitivo lo confirma Odoo (RN06).
import type { Product, SaleLineRequest } from './models'

export interface CartLine {
  productId: number
  name: string
  quantity: number
  /** Precio unitario sin impuestos mostrado en el catálogo. */
  unitPrice: number
}

export interface Cart {
  readonly lines: readonly CartLine[]
}

export const MAX_QUANTITY = 100_000

export const emptyCart: Cart = { lines: [] }

function isValidQuantity(quantity: number): boolean {
  return Number.isFinite(quantity)
}

/** Agrega el producto; si ya está en el carrito suma la cantidad. */
export function addProduct(cart: Cart, product: Pick<Product, 'id' | 'name' | 'price'>, quantity = 1): Cart {
  if (!isValidQuantity(quantity) || quantity <= 0) return cart
  const existing = cart.lines.find((line) => line.productId === product.id)
  if (existing) {
    return changeQuantity(cart, product.id, existing.quantity + quantity)
  }
  const line: CartLine = { productId: product.id, name: product.name, quantity: Math.min(quantity, MAX_QUANTITY), unitPrice: product.price }
  return { lines: [...cart.lines, line] }
}

/** Cambia la cantidad; 0 o menos quita la línea (RN02). Valores no numéricos se ignoran. */
export function changeQuantity(cart: Cart, productId: number, quantity: number): Cart {
  if (!isValidQuantity(quantity)) return cart
  if (quantity <= 0) return removeLine(cart, productId)
  const capped = Math.min(quantity, MAX_QUANTITY)
  return { lines: cart.lines.map((line) => (line.productId === productId ? { ...line, quantity: capped } : line)) }
}

export function increase(cart: Cart, productId: number): Cart {
  const line = cart.lines.find((l) => l.productId === productId)
  return line ? changeQuantity(cart, productId, line.quantity + 1) : cart
}

export function decrease(cart: Cart, productId: number): Cart {
  const line = cart.lines.find((l) => l.productId === productId)
  return line ? changeQuantity(cart, productId, line.quantity - 1) : cart
}

export function removeLine(cart: Cart, productId: number): Cart {
  return { lines: cart.lines.filter((line) => line.productId !== productId) }
}

/** Redondeo a 2 decimales para mostrar (evita 0.1 + 0.2 = 0.30000000000000004). */
function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}

export function lineSubtotal(line: CartLine): number {
  return round2(line.unitPrice * line.quantity)
}

/** Total provisional sin impuestos. */
export function cartTotal(cart: Cart): number {
  return round2(cart.lines.reduce((sum, line) => sum + lineSubtotal(line), 0))
}

export function itemCount(cart: Cart): number {
  return cart.lines.reduce((sum, line) => sum + line.quantity, 0)
}

export function isEmpty(cart: Cart): boolean {
  return cart.lines.length === 0
}

/** Solo productos y cantidades viajan a la API: el precio lo pone Odoo. */
export function toSaleLines(cart: Cart): SaleLineRequest[] {
  return cart.lines.map((line) => ({ productId: line.productId, quantity: line.quantity }))
}
