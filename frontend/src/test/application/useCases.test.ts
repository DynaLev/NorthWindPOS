import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../application/errors'
import type { SessionStorePort } from '../../application/ports'
import { login } from '../../application/useCases/auth'
import { checkout } from '../../application/useCases/checkout'
import { addProduct, emptyCart } from '../../domain/cart'
import { fakeApi, products, sale } from '../fakes'

const cart = addProduct(addProduct(emptyCart, products[0]), products[1])

describe('Caso de uso checkout', () => {
  it('envía solo productos, cantidades, sesión y el identificador de operación', async () => {
    const createSale = vi.fn(async () => sale())
    const result = await checkout(fakeApi({ createSale }), { cart, operationId: 'op-1', sessionId: 1 })
    expect(createSale).toHaveBeenCalledWith({
      clientOperationId: 'op-1', sessionId: 1, lines: [{ productId: 3, quantity: 1 }, { productId: 4, quantity: 1 }],
    })
    expect(result.kind).toBe('success')
    expect(result.message).toContain('10.35 USD')
  })

  it('no llama a la API con el carrito vacío (RN01)', async () => {
    const createSale = vi.fn()
    const result = await checkout(fakeApi({ createSale }), { cart: emptyCart, operationId: 'op' })
    expect(result.kind).toBe('error')
    expect(createSale).not.toHaveBeenCalled()
  })

  it('replay informa que no se duplicó', async () => {
    const result = await checkout(fakeApi({ createSale: async () => sale({ replay: true }) }), { cart, operationId: 'op' })
    expect(result.kind).toBe('replay')
    expect(result.message).toContain('No se duplicó')
  })

  it('504 SALE_RESULT_UNCERTAIN es incierto, nunca éxito', async () => {
    const api = fakeApi({ createSale: async () => { throw new ApiError(504, 'SALE_RESULT_UNCERTAIN', 'No se pudo confirmar.') } })
    const result = await checkout(api, { cart, operationId: 'op' })
    expect(result.kind).toBe('uncertain')
  })

  it('400 de validación es error definitivo', async () => {
    const api = fakeApi({ createSale: async () => { throw new ApiError(400, 'QUANTITY_INVALID', 'Cantidad inválida.') } })
    const result = await checkout(api, { cart, operationId: 'op' })
    expect(result).toEqual({ kind: 'error', message: 'Cantidad inválida.' })
  })
})

describe('Caso de uso login (RF01)', () => {
  it('guarda solo el JWT y el nombre del cajero', async () => {
    const store: SessionStorePort = { readToken: () => null, readOperatorName: () => '', save: vi.fn(), clear: vi.fn() }
    await login(fakeApi(), store, ' cajero ', 'Cajero123!')
    expect(store.save).toHaveBeenCalledWith('t', 'Cajero')
  })
})
