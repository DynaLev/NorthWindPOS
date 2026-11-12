import type { PosApiPort, SessionStorePort } from '../ports'

/** Login del cajero: la API .NET emite el JWT; solo ese token se guarda en el navegador (RF01). */
export async function login(api: PosApiPort, store: SessionStorePort, username: string, password: string) {
  const token = await api.login(username.trim(), password)
  store.save(token.accessToken, token.operator.displayName || token.operator.username)
  return token.operator
}

export function logout(store: SessionStorePort): void {
  store.clear()
}
