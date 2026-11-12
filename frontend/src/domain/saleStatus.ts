const LABELS: Record<string, string> = {
  draft: 'Borrador (sin pagar)',
  paid: 'Pagada',
  done: 'Pagada y contabilizada',
  invoiced: 'Facturada',
  cancel: 'Cancelada',
}

/** Traduce el estado de pos.order para el cajero (RNF03). */
export function describeSaleState(state: string): string {
  return LABELS[state] ?? state
}

/** Disponibilidad legible de un producto del catálogo (RF02). */
export function describeAvailability(product: { tracksInventory: boolean; availableQuantity: number | null; unit: string }): string {
  if (!product.tracksInventory || product.availableQuantity === null) return 'Sin control de stock'
  if (product.availableQuantity <= 0) return 'Sin stock'
  return `Disponible: ${product.availableQuantity} ${product.unit}`
}
