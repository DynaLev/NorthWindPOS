import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { describeError } from '../../application/errors'
import { loadSaleDetail } from '../../application/useCases/sales'
import type { Sale } from '../../domain/models'
import { formatMoney } from '../../domain/money'
import { describeSaleState } from '../../domain/saleStatus'
import { ErrorAlert } from '../components/ErrorAlert'
import { useServices } from '../ServicesContext'

export function SaleDetailPage() {
  const { api } = useServices()
  const params = useParams()
  const [sale, setSale] = useState<Sale | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    loadSaleDetail(api, Number(params.id))
      .then((result) => active && setSale(result))
      .catch((err) => active && setError(describeError(err, 'No se pudo leer la venta.')))
    return () => {
      active = false
    }
  }, [api, params.id])

  return (
    <section>
      <p><Link to="/ventas">Volver al historial</Link></p>
      <h1>{sale ? sale.orderName || `Venta ${sale.orderId}` : 'Venta'}</h1>
      <ErrorAlert message={error} />
      {sale ? (
        <>
          <p>
            Estado: {describeSaleState(sale.state)}. Operación {sale.operationId}.
            Total {formatMoney(sale.total, sale.currency)} (impuestos {formatMoney(sale.tax, sale.currency)}).
            Pagado {formatMoney(sale.paid, sale.currency)}.
          </p>
          <h2>Líneas</h2>
          <ul>
            {sale.lines.map((line) => (
              <li key={`${line.productId}-${line.name}`}>
                {line.name} × {line.quantity} = {formatMoney(line.subtotalWithTax, sale.currency)}
              </li>
            ))}
          </ul>
          <h2>Pagos</h2>
          <ul>
            {sale.payments.map((payment) => (
              <li key={payment.id}>
                {payment.paymentMethodName}{payment.isCash ? ' (efectivo)' : ''}: {formatMoney(payment.amount, sale.currency)}
              </li>
            ))}
          </ul>
        </>
      ) : null}
    </section>
  )
}
