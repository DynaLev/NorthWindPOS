import { afterEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { createHttpPosApi } from '../../infrastructure/http/httpPosApi'
import { createBrowserSessionStore } from '../../infrastructure/storage/browserSessionStore'
import { LoginPage } from '../../presentation/pages/LoginPage'
import { ServicesProvider } from '../../presentation/ServicesContext'

function renderLogin() {
  const store = createBrowserSessionStore()
  const services = { api: createHttpPosApi({ store }), session: store, newOperationId: () => 'op' }
  render(
    <ServicesProvider services={services}>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/pos" element={<p>Caja abierta</p>} />
        </Routes>
      </MemoryRouter>
    </ServicesProvider>,
  )
}

describe('RF01 - Login', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    sessionStorage.clear()
  })

  it('401: muestra credenciales inválidas y no guarda token', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ title: 'AUTH_INVALID_CREDENTIALS', detail: 'Usuario o contraseña inválidos.' }), { status: 401 })))
    const user = userEvent.setup()
    renderLogin()
    await user.type(screen.getByLabelText('Contraseña'), 'mala')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Usuario o contraseña inválidos.')
    expect(sessionStorage.getItem('northwindpos.token')).toBeNull()
  })

  it('200: guarda el JWT y entra a la caja', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({
      accessToken: 'jwt', expiresAtUtc: '', operator: { username: 'cajero', displayName: 'Cajero Northwind', role: 'Cashier' },
    }), { status: 200 })))
    const user = userEvent.setup()
    renderLogin()
    await user.type(screen.getByLabelText('Contraseña'), 'Cajero123!')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))
    expect(await screen.findByText('Caja abierta')).toBeInTheDocument()
    expect(sessionStorage.getItem('northwindpos.token')).toBe('jwt')
  })
})
