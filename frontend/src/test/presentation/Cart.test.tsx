import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { addProduct, changeQuantity, decrease, emptyCart, increase, removeLine } from '../../domain/cart'
import { Cart } from '../../presentation/components/Cart'
import { products } from '../fakes'

function Harness() {
  const [cart, setCart] = useState(addProduct(addProduct(emptyCart, products[0]), products[1]))
  return (
    <Cart
      cart={cart}
      onIncrease={(id) => setCart((c) => increase(c, id))}
      onDecrease={(id) => setCart((c) => decrease(c, id))}
      onChangeQuantity={(id, q) => setCart((c) => changeQuantity(c, id, q))}
      onRemove={(id) => setCart((c) => removeLine(c, id))}
      onCheckout={() => {}}
    />
  )
}

describe('P07 - Carrito en pantalla', () => {
  it('muestra subtotales, total sin impuestos y responde a +1, campo numérico, − y Quitar', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    expect(screen.getByText('Total provisional (sin impuestos): $6.50')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Aumentar Cafe Norte' }))
    expect(screen.getByText('Subtotal: $5.00')).toBeInTheDocument()
    expect(screen.getByText('Total provisional (sin impuestos): $9.00')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Cantidad de Sandwich Norte'), { target: { value: '3' } })
    expect(screen.getByText('Subtotal: $12.00')).toBeInTheDocument()
    expect(screen.getByText('Total provisional (sin impuestos): $17.00')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Disminuir Cafe Norte' }))
    await user.click(screen.getByRole('button', { name: 'Disminuir Cafe Norte' }))
    expect(screen.queryByText('Cafe Norte')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Quitar Sandwich Norte' }))
    expect(screen.getByText('Todavía no hay productos.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cobrar en efectivo' })).toBeDisabled()
  })

  it('el botón de aumentar dice +1 (no "Agregar")', () => {
    render(<Harness />)
    expect(screen.getAllByRole('button', { name: /Aumentar/ })[0]).toHaveTextContent('+1')
  })
})
