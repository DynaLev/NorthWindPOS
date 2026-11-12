import { describe, expect, it } from 'vitest'
import {
  addProduct, cartTotal, changeQuantity, decrease, emptyCart, increase, itemCount, lineSubtotal, removeLine, toSaleLines,
} from '../../domain/cart'

const cafe = { id: 3, name: 'Cafe Norte', price: 2.5 }
const sandwich = { id: 4, name: 'Sandwich Norte', price: 4 }

describe('P07 - Dominio del carrito (funciones puras)', () => {
  it('agrega un producto nuevo con cantidad 1', () => {
    const cart = addProduct(emptyCart, cafe)
    expect(cart.lines).toEqual([{ productId: 3, name: 'Cafe Norte', quantity: 1, unitPrice: 2.5 }])
    expect(emptyCart.lines).toHaveLength(0) // inmutable
  })

  it('agregar el mismo producto suma cantidad en la misma línea', () => {
    const cart = addProduct(addProduct(emptyCart, cafe), cafe)
    expect(cart.lines).toHaveLength(1)
    expect(cart.lines[0].quantity).toBe(2)
  })

  it('maneja varias líneas con subtotales y total', () => {
    let cart = addProduct(emptyCart, cafe)
    cart = addProduct(cart, sandwich)
    cart = changeQuantity(cart, 3, 2)
    expect(cart.lines).toHaveLength(2)
    expect(lineSubtotal(cart.lines[0])).toBe(5)
    expect(lineSubtotal(cart.lines[1])).toBe(4)
    expect(cartTotal(cart)).toBe(9)
    expect(itemCount(cart)).toBe(3)
  })

  it('aumenta y disminuye; bajar a 0 quita la línea', () => {
    let cart = addProduct(emptyCart, cafe)
    cart = increase(cart, 3)
    expect(cart.lines[0].quantity).toBe(2)
    cart = decrease(cart, 3)
    cart = decrease(cart, 3)
    expect(cart.lines).toHaveLength(0)
    expect(cartTotal(cart)).toBe(0)
  })

  it('cantidad 0 o negativa quita la línea; NaN se ignora', () => {
    const cart = addProduct(addProduct(emptyCart, cafe), sandwich)
    expect(changeQuantity(cart, 3, 0).lines.map((l) => l.productId)).toEqual([4])
    expect(changeQuantity(cart, 4, -5).lines.map((l) => l.productId)).toEqual([3])
    expect(changeQuantity(cart, 3, Number.NaN)).toBe(cart)
  })

  it('quita una línea concreta', () => {
    const cart = removeLine(addProduct(addProduct(emptyCart, cafe), sandwich), 3)
    expect(cart.lines.map((l) => l.productId)).toEqual([4])
  })

  it('redondea el total a 2 decimales', () => {
    const cart = changeQuantity(addProduct(emptyCart, { id: 1, name: 'X', price: 0.1 }), 1, 3)
    expect(cartTotal(cart)).toBe(0.3)
  })

  it('solo envía producto y cantidad a la API (el precio lo pone Odoo)', () => {
    const cart = changeQuantity(addProduct(addProduct(emptyCart, cafe), sandwich), 3, 2)
    expect(toSaleLines(cart)).toEqual([{ productId: 3, quantity: 2 }, { productId: 4, quantity: 1 }])
  })
})
