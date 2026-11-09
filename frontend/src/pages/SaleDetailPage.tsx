import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { ErrorAlert } from '../components/ErrorAlert'
import type { Sale } from '../types/pos'

function money(value: number) {
  return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function SaleDetailPage() {
  const params = useParams()
  const [sale, setSale] = useState<Sale | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    const id = Number(params.id)
    if (!Number.isFinite(id)) {
      setError('Identificador de venta inválido.')
      return
    }
    api.sale(id)
      .then(setSale)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'No se pudo leer la venta.'))
  }, [params.id])

  return (
    <section>
      <p><Link to="/ventas">Volver al historial</Link></p>
      <h1>{sale ? sale.orderName || `Venta ${sale.orderId}` : 'Venta'}</h1>
      <ErrorAlert message={error} />
      {sale ? (
        <>
          <p>Estado {sale.state}. Operación {sale.operationId}. Total ${money(sale.total)}. Pagado ${money(sale.paid)}.</p>
          <ul>
            {sale.lines.map((line) => (
              <li key={`${line.productId}-${line.name}`}>
                {line.name} × {line.quantity} = ${money(line.subtotalWithTax)}
              </li>
            ))}
          </ul>
          <ul>
            {sale.payments.map((payment) => (
              <li key={payment.id}>{payment.paymentMethodName}: ${money(payment.amount)}</li>
            ))}
          </ul>
        </>
      ) : null}
    </section>
  )
}
