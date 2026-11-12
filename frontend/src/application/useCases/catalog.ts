import type { PosSession, Product } from '../../domain/models'
import type { PosApiPort } from '../ports'

export interface CatalogState {
  session: PosSession | null
  sessions: PosSession[]
  products: Product[]
}

/** Carga la sesión activa, las sesiones abiertas y los productos (RF02, RF04). */
export async function loadCatalog(api: PosApiPort, search: string): Promise<CatalogState> {
  const [session, sessions, products] = await Promise.all([
    api.getActiveSession(),
    api.listOpenSessions().catch(() => [] as PosSession[]),
    api.searchProducts(search),
  ])
  return { session, sessions: sessions.length > 0 ? sessions : [session], products }
}

export function searchProducts(api: PosApiPort, search: string): Promise<Product[]> {
  return api.searchProducts(search)
}
