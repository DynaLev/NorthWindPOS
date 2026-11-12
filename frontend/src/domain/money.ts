/** Formatea un importe con 2 decimales y el código de moneda si se conoce. */
export function formatMoney(value: number, currency = ''): string {
  const amount = value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return currency ? `${amount} ${currency}` : `$${amount}`
}
