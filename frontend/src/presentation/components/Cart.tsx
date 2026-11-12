import { cartTotal, lineSubtotal, type Cart as CartModel } from '../../domain/cart'
import { formatMoney } from '../../domain/money'
import { QuantityInput } from './QuantityInput'

interface CartProps {
  cart: CartModel
  currency?: string
  onIncrease: (productId: number) => void
  onDecrease: (productId: number) => void
  onChangeQuantity: (productId: number, quantity: number) => void
  onRemove: (productId: number) => void
  onCheckout: () => void
  busy?: boolean
  canCheckout?: boolean
}

export function Cart({ cart, currency = '', onIncrease, onDecrease, onChangeQuantity, onRemove, onCheckout, busy, canCheckout = true }: CartProps) {
  const total = cartTotal(cart)

  return (
    <section className="cart" aria-label="Carrito">
      <h2>Carrito</h2>
      {cart.lines.length === 0 ? <p className="muted">Todavía no hay productos.</p> : null}
      <ul>
        {cart.lines.map((line) => (
          <li key={line.productId} className="cart-line">
            <div>
              <strong>{line.name}</strong>
              <span className="muted"> {formatMoney(line.unitPrice, currency)} c/u</span>
            </div>
            <div className="qty">
              <button type="button" onClick={() => onDecrease(line.productId)} aria-label={`Disminuir ${line.name}`}>
                −
              </button>
              <QuantityInput
                label={`Cantidad de ${line.name}`}
                quantity={line.quantity}
                onCommit={(quantity) => onChangeQuantity(line.productId, quantity)}
              />
              <button type="button" onClick={() => onIncrease(line.productId)} aria-label={`Aumentar ${line.name}`}>
                +1
              </button>
              <button type="button" onClick={() => onRemove(line.productId)} aria-label={`Quitar ${line.name}`}>
                Quitar
              </button>
            </div>
            <p className="subtotal">Subtotal: {formatMoney(lineSubtotal(line), currency)}</p>
          </li>
        ))}
      </ul>
      <p className="total">Total provisional (sin impuestos): {formatMoney(total, currency)}</p>
      <p className="muted">El total final con impuestos lo calcula y confirma Odoo al cobrar.</p>
      <button type="button" className="pay" onClick={onCheckout} disabled={busy || cart.lines.length === 0 || !canCheckout}>
        {busy ? 'Cobrando…' : 'Cobrar en efectivo'}
      </button>
    </section>
  )
}
