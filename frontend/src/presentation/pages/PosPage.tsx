import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { addProduct, changeQuantity, decrease, emptyCart, increase, removeLine, type Cart as CartModel } from '../../domain/cart'
import type { PosSession, Product } from '../../domain/models'
import { describeError } from '../../application/errors'
import { checkout } from '../../application/useCases/checkout'
import { loadCatalog, searchProducts } from '../../application/useCases/catalog'
import { Cart } from '../components/Cart'
import { ErrorAlert } from '../components/ErrorAlert'
import { ProductCard } from '../components/ProductCard'
import { SessionPicker } from '../components/SessionPicker'
import { useServices } from '../ServicesContext'

export function PosPage() {
  const { api, newOperationId } = useServices()
  const [sessions, setSessions] = useState<PosSession[]>([])
  const [sessionId, setSessionId] = useState<number | undefined>()
  const [products, setProducts] = useState<Product[]>([])
  const [loaded, setLoaded] = useState(false)
  const [search, setSearch] = useState('')
  const [cart, setCart] = useState<CartModel>(emptyCart)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  // El mismo id se conserva hasta que Odoo confirma la venta (RN07).
  const [operationId, setOperationId] = useState(newOperationId)

  useEffect(() => {
    let active = true
    loadCatalog(api, '')
      .then((state) => {
        if (!active) return
        setSessions(state.sessions)
        setSessionId(state.session?.id)
        setProducts(state.products)
      })
      .catch((err) => active && setError(describeError(err, 'No se pudo consultar Odoo.')))
      .finally(() => active && setLoaded(true))
    return () => {
      active = false
    }
  }, [api])

  async function onSearch() {
    setError('')
    try {
      setProducts(await searchProducts(api, search))
    } catch (err) {
      setError(describeError(err, 'No se pudo consultar Odoo.'))
    }
  }

  function updateCart(next: CartModel) {
    setNotice('')
    setCart(next)
  }

  async function onCheckout() {
    setBusy(true)
    setError('')
    setNotice('')
    const result = await checkout(api, { cart, operationId, sessionId })
    if (result.kind === 'success' || result.kind === 'replay') {
      setCart(emptyCart)
      setOperationId(newOperationId())
      setNotice(result.message)
    } else {
      setError(result.message)
    }
    setBusy(false)
  }

  const session = sessions.find((s) => s.id === sessionId)
  const currency = session?.currency ?? ''

  return (
    <div className="workspace">
      <section>
        <form className="search" onSubmit={(event) => { event.preventDefault(); void onSearch() }}>
          <label>
            Buscar por nombre o código
            <input value={search} onChange={(e) => setSearch(e.target.value)} />
          </label>
          <button type="submit">Buscar</button>
        </form>
        <ErrorAlert message={error} />
        {notice ? <p className="notice" role="status">{notice}</p> : null}
        <SessionPicker sessions={sessions} selectedId={sessionId} onSelect={setSessionId} />
        <div className="catalog">
          {products.map((product) => (
            <ProductCard key={product.id} product={product} currency={currency} onAdd={(p) => updateCart(addProduct(cart, p))} />
          ))}
        </div>
        {loaded && products.length === 0 && !error ? <p>No hay productos de POS para esa búsqueda.</p> : null}
      </section>
      <Cart
        cart={cart}
        currency={currency}
        busy={busy}
        canCheckout={Boolean(session)}
        onIncrease={(id) => updateCart(increase(cart, id))}
        onDecrease={(id) => updateCart(decrease(cart, id))}
        onChangeQuantity={(id, qty) => updateCart(changeQuantity(cart, id, qty))}
        onRemove={(id) => updateCart(removeLine(cart, id))}
        onCheckout={() => void onCheckout()}
      />
      <p className="muted">
        Los precios del catálogo y el total del carrito son provisionales y sin impuestos. El importe cobrado lo confirma Odoo.{' '}
        <Link to="/ventas">Ver ventas</Link>
      </p>
    </div>
  )
}
