// Puertos de la capa de aplicación. Infrastructure los implementa; los casos de uso
// dependen solo de estas interfaces (nunca de fetch ni de Odoo).
import type { AuthToken, PosSession, Product, Sale, SaleLineRequest, SaleSummary } from '../domain/models'

export interface CreateSaleInput {
  clientOperationId: string
  sessionId?: number
  lines: SaleLineRequest[]
}

export interface PosApiPort {
  login(username: string, password: string): Promise<AuthToken>
  searchProducts(search: string): Promise<Product[]>
  getActiveSession(): Promise<PosSession>
  listOpenSessions(): Promise<PosSession[]>
  createSale(input: CreateSaleInput): Promise<Sale>
  listSales(): Promise<SaleSummary[]>
  getSale(id: number): Promise<Sale>
}

export interface SessionStorePort {
  readToken(): string | null
  readOperatorName(): string
  save(token: string, displayName: string): void
  clear(): void
}
