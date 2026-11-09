const KEY = 'northwindpos.token'
const NAME = 'northwindpos.operator'

export function readToken(): string | null {
  return sessionStorage.getItem(KEY)
}

export function readOperatorName(): string {
  return sessionStorage.getItem(NAME) ?? ''
}

export function saveSession(token: string, displayName: string) {
  sessionStorage.setItem(KEY, token)
  sessionStorage.setItem(NAME, displayName)
}

export function clearToken() {
  sessionStorage.removeItem(KEY)
  sessionStorage.removeItem(NAME)
}
