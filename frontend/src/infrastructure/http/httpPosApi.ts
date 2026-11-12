// Adaptador HTTP hacia la API .NET. Solo conoce rutas /api/...; nunca llama a Odoo (FE-2).
import { ApiError } from '../../application/errors'
import type { CreateSaleInput, PosApiPort, SessionStorePort } from '../../application/ports'
import type { AuthToken, PosSession, Product, Sale, SaleSummary } from '../../domain/models'

type FetchLike = (input: string, init?: RequestInit) => Promise<Response>

export interface HttpPosApiOptions {
  store: SessionStorePort
  /** Por defecto usa globalThis.fetch en cada llamada (permite simularlo en pruebas). */
  fetchFn?: FetchLike
  baseUrl?: string
}

export function createHttpPosApi({ store, fetchFn, baseUrl = '' }: HttpPosApiOptions): PosApiPort {
  const doFetch: FetchLike = (input, init) => (fetchFn ?? globalThis.fetch)(input, init)

  async function request<T>(path: string, init: RequestInit = {}, auth = true): Promise<T> {
    const headers = buildHeaders(init, auth ? store.readToken() : null, auth)
    let response: Response
    try {
      response = await doFetch(`${baseUrl}${path}`, { ...init, headers })
    } catch {
      throw new ApiError(0, 'NETWORK', 'No hay conexión con la API .NET.')
    }
    if (response.status === 401 && auth) store.clear()
    if (!response.ok) throw await toApiError(response)
    return (response.status === 204 ? undefined : await response.json()) as T
  }

  return {
    login: (username, password) =>
      request<AuthToken>('/api/auth/login', { method: 'POST', body: JSON.stringify({ username, password }) }, false),
    searchProducts: (search) => {
      const term = search.trim()
      return request<Product[]>(`/api/products${term ? `?search=${encodeURIComponent(term)}` : ''}`)
    },
    getActiveSession: () => request<PosSession>('/api/pos/sessions/active'),
    listOpenSessions: () => request<PosSession[]>('/api/pos/sessions'),
    createSale: (input: CreateSaleInput) =>
      request<Sale>('/api/sales', {
        method: 'POST',
        body: JSON.stringify({ clientOperationId: input.clientOperationId, sessionId: input.sessionId ?? null, lines: input.lines }),
      }),
    listSales: () => request<SaleSummary[]>('/api/sales'),
    getSale: (id) => request<Sale>(`/api/sales/${id}`),
  }
}

function buildHeaders(init: RequestInit, token: string | null, auth: boolean): Headers {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (auth) {
    if (!token) throw new ApiError(401, 'UNAUTHORIZED', 'Inicie sesión para continuar.')
    headers.set('Authorization', `Bearer ${token}`)
  }
  return headers
}

async function toApiError(response: Response): Promise<ApiError> {
  let code = 'HTTP_ERROR'
  let message = `La API respondió ${response.status}.`
  try {
    const problem = await response.json()
    code = problem.title || code
    message = problem.detail || problem.title || message
  } catch {
    // cuerpo vacío o no JSON
  }
  return new ApiError(response.status, code, message)
}
