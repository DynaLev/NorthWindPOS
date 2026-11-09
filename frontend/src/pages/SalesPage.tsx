import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { ErrorAlert } from '../components/ErrorAlert'
import type { SaleSummary } from '../types/pos'

function money(value: number) {
  return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function SalesPage() {
  const [sales, setSales] = useState<SaleSummary[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    api.sales()
      .then(setSales)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo leer el historial.'))
  }, [])

  return (
    <section>
      <h1>Ventas</h1>
      <ErrorAlert message={error} />
      {sales.length === 0 && !error ? <p>Aún no hay ventas en Odoo.</p> : null}
      <table>
        <thead>
          <tr>
            <th>Orden</th>
            <th>Estado</th>
            <th>Total</th>
            <th>Pago</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {sales.map((sale) => (
            <tr key={sale.orderId}>
              <td>{sale.orderName || `#${sale.orderId}`}</td>
              <td>{sale.state}</td>
              <td>${money(sale.total)}</td>
              <td>{sale.paymentMethodName || '—'}</td>
              <td><Link to={`/ventas/${sale.orderId}`}>Detalle</Link></td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
