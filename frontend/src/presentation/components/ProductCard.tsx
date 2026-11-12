import type { Product } from '../../domain/models'
import { formatMoney } from '../../domain/money'
import { describeAvailability } from '../../domain/saleStatus'

interface ProductCardProps {
  product: Product
  currency: string
  onAdd: (product: Product) => void
}

export function ProductCard({ product, currency, onAdd }: ProductCardProps) {
  const money = formatMoney(product.price, product.currency || currency)
  const availability = describeAvailability(product)
  return (
    <button
      type="button"
      className="product"
      onClick={() => onAdd(product)}
      aria-label={`Agregar ${product.name}, ${money} sin impuestos, ${availability}`}
    >
      <span>{product.name}</span>
      <small>{product.code || 'sin código'}</small>
      <strong>{money}</strong>
      <small>sin impuestos</small>
      <small className={product.tracksInventory && (product.availableQuantity ?? 0) <= 0 ? 'warning' : undefined}>{availability}</small>
    </button>
  )
}
