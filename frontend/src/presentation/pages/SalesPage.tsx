import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { describeError } from '../../application/errors'
import { loadSales } from '../../application/useCases/sales'
import type { SaleSummary } from '../../domain/models'
import { formatMoney } from '../../domain/money'
import { describeSaleState } from '../../domain/saleStatus'
import { ErrorAlert } from '../components/ErrorAlert'
import { useServices } from '../ServicesContext'

export function SalesPage() {
  const { api } = useServices()
  const [sales, setSales] = useState<SaleSummary[]>([])
  const [loaded, setLoaded] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    loadSales(api)
      .then((list) => active && setSales(list))
      .catch((err) => active && setError(describeError(err, 'No se pudo leer el historial.')))
      .finally(() => active && setLoaded(true))
    return () => {
      active = false
    }
  }, [api])

  return (
    <section>
      <h1>Ventas</h1>
      <ErrorAlert message={error} />
      {loaded && sales.length === 0 && !error ? <p>Aún no hay ventas en Odoo.</p> : null}
      <table>
        <caption className="sr-only">Historial de ventas registradas en Odoo</caption>
        <thead>
          <tr>
            <th scope="col">Orden</th>
            <th scope="col">Estado</th>
            <th scope="col">Total</th>
            <th scope="col">Pago</th>
            <th scope="col">Acciones</th>
          </tr>
        </thead>
        <tbody>
          {sales.map((sale) => (
            <tr key={sale.orderId}>
              <td>{sale.orderName || `#${sale.orderId}`}</td>
              <td>{describeSaleState(sale.state)}</td>
              <td>{formatMoney(sale.total, sale.currency)}</td>
              <td>{sale.paymentMethodName || '—'}</td>
              <td><Link to={`/ventas/${sale.orderId}`} aria-label={`Ver detalle de ${sale.orderName || sale.orderId}`}>Detalle</Link></td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
