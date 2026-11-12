// Composición por defecto: adaptadores reales del navegador.
import type { PosApiPort, SessionStorePort } from '../application/ports'
import { createHttpPosApi } from './http/httpPosApi'
import { createBrowserSessionStore } from './storage/browserSessionStore'

export interface Services {
  api: PosApiPort
  session: SessionStorePort
  newOperationId: () => string
}

export function createDefaultServices(): Services {
  const session = createBrowserSessionStore()
  return {
    api: createHttpPosApi({ store: session }),
    session,
    newOperationId: () => crypto.randomUUID(),
  }
}
