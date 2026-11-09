import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { Cart } from '../components/Cart'
import { ErrorAlert } from '../components/ErrorAlert'
import type { CartLine, PosSession, Product, Sale } from '../types/pos'

function money(value: number) {
  return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function PosPage() {
  const [session, setSession] = useState<PosSession | null>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [search, setSearch] = useState('')
  const [lines, setLines] = useState<CartLine[]>([])
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [operationId, setOperationId] = useState(() => crypto.randomUUID())

  async function load(term = search) {
    setError('')
    try {
      const [active, catalog] = await Promise.all([api.activeSession(), api.products(term)])
      setSession(active)
      setProducts(catalog)
    } catch (err) {
      setSession(null)
      setError(err instanceof ApiError ? err.message : 'No se pudo consultar Odoo.')
    }
  }

  useEffect(() => {
    void load('')
  }, [])

  function addProduct(product: Product) {
    setNotice('')
    setLines((current) => {
      const existing = current.find((line) => line.productId === product.id)
      if (existing) {
        return current.map((line) =>
          line.productId === product.id ? { ...line, quantity: line.quantity + 1 } : line,
        )
      }
      return [...current, { productId: product.id, name: product.name, quantity: 1, unitPrice: product.price }]
    })
  }

  function changeQty(productId: number, quantity: number) {
    if (quantity <= 0) {
      setLines((current) => current.filter((line) => line.productId !== productId))
      return
    }
    setLines((current) => current.map((line) => (line.productId === productId ? { ...line, quantity } : line)))
  }

  async function checkout() {
    if (lines.length === 0) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const sale: Sale = await api.createSale(
        operationId,
        lines.map((line) => ({ productId: line.productId, quantity: line.quantity })),
        session?.id,
      )
      setLines([])
      setOperationId(crypto.randomUUID())
      setNotice(
        sale.replay
          ? `Esa operación ya estaba registrada como ${sale.orderName}. No se duplicó la venta.`
          : `Venta ${sale.orderName} registrada en Odoo. Estado ${sale.state}. Total ${money(sale.total)}.`,
      )
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'No se pudo cobrar.'
      const uncertain = err instanceof ApiError && (err.code === 'SALE_RESULT_UNCERTAIN' || err.status === 504)
      setError(
        uncertain
          ? `${message} Puede volver a cobrar: se reutilizará el mismo identificador.`
          : message,
      )
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="workspace">
      <section>
        <form
          className="search"
          onSubmit={(event) => {
            event.preventDefault()
            void load(search)
          }}
        >
          <label>
            Buscar por nombre o código
            <input value={search} onChange={(e) => setSearch(e.target.value)} />
          </label>
          <button type="submit">Buscar</button>
        </form>
        <ErrorAlert message={error} />
        {notice ? <p className="notice" role="status">{notice}</p> : null}
        <p className="muted">
          {session
            ? `Sesión ${session.name} · ${session.configName} · efectivo ${session.cashPaymentMethodName ?? 'sin método'}`
            : 'Sin sesión abierta'}
        </p>
        <div className="catalog">
          {products.map((product) => (
            <button type="button" key={product.id} className="product" onClick={() => addProduct(product)}>
              <span>{product.name}</span>
              <small>{product.code || 'sin código'}</small>
              <strong>${money(product.price)}</strong>
            </button>
          ))}
        </div>
        {products.length === 0 ? <p>No hay productos de POS para esa búsqueda.</p> : null}
      </section>
      <Cart
        lines={lines}
        busy={busy}
        onChangeQty={changeQty}
        onRemove={(id) => changeQty(id, 0)}
        onCheckout={() => void checkout()}
      />
      <p className="muted">
        El total de esta pantalla es provisional. El importe cobrado lo confirma Odoo.{' '}
        <Link to="/ventas">Ver ventas</Link>
      </p>
    </div>
  )
}
