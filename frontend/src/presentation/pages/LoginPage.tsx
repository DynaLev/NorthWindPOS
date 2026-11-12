import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { describeError } from '../../application/errors'
import { login } from '../../application/useCases/auth'
import { ErrorAlert } from '../components/ErrorAlert'
import { useServices } from '../ServicesContext'

export function LoginPage() {
  const { api, session } = useServices()
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
      await login(api, session, username, password)
      navigate('/pos')
    } catch (err) {
      setError(describeError(err, 'No se pudo iniciar sesión.'))
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
