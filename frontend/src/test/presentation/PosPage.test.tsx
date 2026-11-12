import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { createHttpPosApi } from '../../infrastructure/http/httpPosApi'
import { createBrowserSessionStore } from '../../infrastructure/storage/browserSessionStore'
import { PosPage } from '../../presentation/pages/PosPage'
import { ServicesProvider } from '../../presentation/ServicesContext'
import { products, sale, session } from '../fakes'

type SaleResponder = () => Promise<Response>

const json = (status: number, body: unknown) =>
  Promise.resolve(new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }))

let saleResponder: SaleResponder
let saleBodies: { clientOperationId: string }[]

function installFetch() {
  saleBodies = []
  vi.stubGlobal('fetch', vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/pos/sessions/active') return json(200, session)
    if (url === '/api/pos/sessions') return json(200, [session])
    if (url.startsWith('/api/products')) return json(200, products)
    if (url === '/api/sales' && init?.method === 'POST') {
      saleBodies.push(JSON.parse(String(init.body)))
      return saleResponder()
    }
    return json(404, { title: 'NOT_FOUND' })
  }))
}

function renderPos() {
  const store = createBrowserSessionStore()
  store.save('jwt-de-prueba', 'Cajero')
  let n = 0
  const services = { api: createHttpPosApi({ store }), session: store, newOperationId: () => `op-${++n}` }
  render(
    <ServicesProvider services={services}>
      <MemoryRouter>
        <PosPage />
      </MemoryRouter>
    </ServicesProvider>,
  )
}

async function addTwoProductsAndPay(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByRole('button', { name: /Agregar Cafe Norte/ }))
  await user.click(screen.getByRole('button', { name: /Agregar Sandwich Norte/ }))
  await user.click(screen.getByRole('button', { name: 'Cobrar en efectivo' }))
}

describe('P08 - PosPage ante fallos de la API (fetch simulado)', () => {
  beforeEach(installFetch)
  afterEach(() => {
    vi.unstubAllGlobals()
    sessionStorage.clear()
  })

  it('muestra disponibilidad y precio sin impuestos en el catálogo (RF02)', async () => {
    renderPos()
    expect(await screen.findByText('Disponible: 7 Unidades')).toBeInTheDocument()
    expect(screen.getByText('Sin control de stock')).toBeInTheDocument()
    expect(screen.getByText('2.50 USD')).toBeInTheDocument()
  })

  it('500 de la API: muestra el detalle y no declara éxito', async () => {
    saleResponder = () => json(500, { title: 'INTERNAL_ERROR', detail: 'Error interno del servidor.' })
    const user = userEvent.setup()
    renderPos()
    await addTwoProductsAndPay(user)
    expect(await screen.findByRole('alert')).toHaveTextContent('Error interno del servidor.')
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(screen.getByText('Cafe Norte', { selector: 'strong' })).toBeInTheDocument() // el carrito se conserva
  })

  it('red caída: avisa que no hay conexión y conserva el identificador', async () => {
    saleResponder = () => Promise.reject(new TypeError('Failed to fetch'))
    const user = userEvent.setup()
    renderPos()
    await addTwoProductsAndPay(user)
    expect(await screen.findByRole('alert')).toHaveTextContent('No hay conexión con la API .NET.')
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('504 SALE_RESULT_UNCERTAIN: mensaje de resultado incierto y reintento con el MISMO id', async () => {
    saleResponder = () => json(504, { title: 'SALE_RESULT_UNCERTAIN', detail: 'No se pudo confirmar si la venta se creó.' })
    const user = userEvent.setup()
    renderPos()
    await addTwoProductsAndPay(user)
    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('No se pudo confirmar si la venta se creó.')
    expect(alert).toHaveTextContent('se reutilizará el mismo identificador')
    expect(screen.queryByRole('status')).not.toBeInTheDocument()

    saleResponder = () => json(200, sale({ replay: true, operationId: 'op-1' }))
    await user.click(screen.getByRole('button', { name: 'Cobrar en efectivo' }))
    expect(await screen.findByRole('status')).toHaveTextContent('No se duplicó la venta')
    expect(saleBodies.map((b) => b.clientOperationId)).toEqual(['op-1', 'op-1'])
  })

  it('200: muestra éxito con el total de Odoo y genera un id nuevo para la siguiente venta', async () => {
    saleResponder = () => json(200, sale())
    const user = userEvent.setup()
    renderPos()
    await addTwoProductsAndPay(user)
    expect(await screen.findByRole('status')).toHaveTextContent('Venta Northwind Caja/0010 registrada en Odoo')
    expect(screen.getByRole('status')).toHaveTextContent('10.35 USD')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    await waitFor(() => expect(screen.getByText('Todavía no hay productos.')).toBeInTheDocument())

    await addTwoProductsAndPay(user)
    await waitFor(() => expect(saleBodies).toHaveLength(2))
    expect(saleBodies[1].clientOperationId).toBe('op-2')
  })

  it('Odoo caído al cargar (503): muestra el mensaje de la API', async () => {
    vi.stubGlobal('fetch', vi.fn(() => json(503, { title: 'ODOO_UNAVAILABLE', detail: 'Odoo no está disponible en este momento.' })))
    renderPos()
    expect(await screen.findByRole('alert')).toHaveTextContent('Odoo no está disponible en este momento.')
  })
})
