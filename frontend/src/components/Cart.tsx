import type { CartLine } from '../types/pos'

interface CartProps {
  lines: CartLine[]
  onChangeQty: (productId: number, quantity: number) => void
  onRemove: (productId: number) => void
  onCheckout: () => void
  busy?: boolean
}

function money(value: number) {
  return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function Cart({ lines, onChangeQty, onRemove, onCheckout, busy }: CartProps) {
  const total = lines.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0)

  return (
    <section className="cart" aria-label="Carrito">
      <h2>Carrito</h2>
      {lines.length === 0 ? <p className="muted">Todavía no hay productos.</p> : null}
      <ul>
        {lines.map((line, index) => (
          <li key={line.productId}>
            <div>
              <strong>{line.name}</strong>
              <span className="muted"> ${money(line.unitPrice)}</span>
            </div>
            <div className="qty">
              <button type="button" onClick={() => onChangeQty(line.productId, line.quantity - 1)} aria-label={`Disminuir ${line.name}`}>
                −
              </button>
              <span>Cantidad: {line.quantity}</span>
              <button
                type="button"
                data-testid={index === 0 ? 'add-test' : undefined}
                onClick={() => onChangeQty(line.productId, line.quantity + 1)}
                aria-label={`Aumentar ${line.name}`}
              >
                Agregar
              </button>
              <button type="button" onClick={() => onRemove(line.productId)}>
                Quitar
              </button>
            </div>
          </li>
        ))}
      </ul>
      <p className="total">Total: ${money(total)}</p>
      <button type="button" className="pay" onClick={onCheckout} disabled={busy || lines.length === 0}>
        {busy ? 'Cobrando…' : 'Cobrar en efectivo'}
      </button>
    </section>
  )
}
