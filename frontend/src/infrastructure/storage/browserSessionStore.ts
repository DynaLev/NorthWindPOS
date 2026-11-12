import type { SessionStorePort } from '../../application/ports'

const TOKEN_KEY = 'northwindpos.token'
const NAME_KEY = 'northwindpos.operator'

/** Guarda solo el JWT de la API .NET en sessionStorage (nunca credenciales de Odoo). */
export function createBrowserSessionStore(storage: Storage = sessionStorage): SessionStorePort {
  return {
    readToken: () => storage.getItem(TOKEN_KEY),
    readOperatorName: () => storage.getItem(NAME_KEY) ?? '',
    save(token, displayName) {
      storage.setItem(TOKEN_KEY, token)
      storage.setItem(NAME_KEY, displayName)
    },
    clear() {
      storage.removeItem(TOKEN_KEY)
      storage.removeItem(NAME_KEY)
    },
  }
}
