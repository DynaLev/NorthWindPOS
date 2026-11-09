import { describe, it, expect } from 'vitest'
import { useState } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Cart } from '../components/Cart'
import type { CartLine } from '../types/pos'

function Harness() {
  const [lines, setLines] = useState<CartLine[]>([
    { productId: 1, name: 'Cafe', quantity: 1, unitPrice: 10 },
  ])
  return (
    <Cart
      lines={lines}
      onChangeQty={(productId, quantity) =>
        setLines((current) => current.map((line) => (line.productId === productId ? { ...line, quantity } : line)))
      }
      onRemove={() => setLines([])}
      onCheckout={() => {}}
    />
  )
}

describe('P07 - Cart updates totals', () => {
  it('updates quantities and total', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    expect(screen.getByText('Total: $10.00')).toBeInTheDocument()
    await user.click(screen.getByTestId('add-test'))
    expect(screen.getByText('Total: $20.00')).toBeInTheDocument()
    expect(screen.getByText('Cantidad: 2')).toBeInTheDocument()
  })
})
