import { clearToken, readToken } from '../auth/session'
import type { AuthToken, PosSession, Product, Sale, SaleSummary } from '../types/pos'

export class ApiError extends Error {
  status: number
  code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.status = status
    this.code = code
  }
}

async function request<T>(path: string, init: RequestInit = {}, auth = true): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  if (auth) {
    const token = readToken()
    if (!token) {
      throw new ApiError(401, 'UNAUTHORIZED', 'Inicie sesión para continuar.')
    }
    headers.set('Authorization', `Bearer ${token}`)
  }

  let response: Response
  try {
    response = await fetch(path, { ...init, headers })
  } catch {
    throw new ApiError(0, 'NETWORK', 'No hay conexión con la API .NET.')
  }

  if (response.status === 401 && auth) {
    clearToken()
  }

  if (!response.ok) {
    let code = 'HTTP_ERROR'
    let message = `La API respondió ${response.status}.`
    try {
      const problem = await response.json()
      code = problem.title || code
      message = problem.detail || problem.title || message
    } catch {
      // cuerpo vacío
    }
    throw new ApiError(response.status, code, message)
  }

  if (response.status === 204) {
    return undefined as T
  }
  return response.json() as Promise<T>
}

export const api = {
  login(username: string, password: string) {
    return request<AuthToken>(
      '/api/auth/login',
      { method: 'POST', body: JSON.stringify({ username, password }) },
      false,
    )
  },
  products(search: string) {
    const query = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : ''
    return request<Product[]>(`/api/products${query}`)
  },
  activeSession() {
    return request<PosSession>('/api/pos/sessions/active')
  },
  createSale(clientOperationId: string, lines: { productId: number; quantity: number }[], sessionId?: number) {
    return request<Sale>('/api/sales', {
      method: 'POST',
      body: JSON.stringify({ clientOperationId, sessionId: sessionId ?? null, lines }),
    })
  },
  sales() {
    return request<SaleSummary[]>('/api/sales')
  },
  sale(id: number) {
    return request<Sale>(`/api/sales/${id}`)
  },
}
