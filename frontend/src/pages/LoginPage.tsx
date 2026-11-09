import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { saveSession } from '../auth/session'
import { ErrorAlert } from '../components/ErrorAlert'

export function LoginPage() {
  const navigate = useNavigate()
  const [username, setUsername] = useState('cajero')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setError('')
    setBusy(true)
    try {
      const token = await api.login(username, password)
      saveSession(token.accessToken, token.operator.displayName || token.operator.username)
      navigate('/pos')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo iniciar sesión.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="login">
      <form onSubmit={onSubmit}>
        <p className="eyebrow">NorthWind POS</p>
        <h1>Caja</h1>
        <label>
          Usuario
          <input value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" required />
        </label>
        <label>
          Contraseña
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" required />
        </label>
        <ErrorAlert message={error} />
        <button type="submit" disabled={busy}>{busy ? 'Entrando…' : 'Entrar'}</button>
      </form>
    </main>
  )
}
