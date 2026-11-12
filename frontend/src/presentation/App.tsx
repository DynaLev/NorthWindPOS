import type { ReactNode } from 'react'
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { logout } from '../application/useCases/auth'
import { LoginPage } from './pages/LoginPage'
import { PosPage } from './pages/PosPage'
import { SaleDetailPage } from './pages/SaleDetailPage'
import { SalesPage } from './pages/SalesPage'
import { useServices } from './ServicesContext'

function RequireAuth({ children }: { children: ReactNode }) {
  const { session } = useServices()
  if (!session.readToken()) return <Navigate to="/login" replace />
  return <>{children}</>
}

function Shell({ children }: { children: ReactNode }) {
  const { session } = useServices()
  const navigate = useNavigate()
  return (
    <div className="pos-shell">
      <header>
        <div>
          <p className="eyebrow">NorthWind</p>
          <strong>Punto de venta</strong>
        </div>
        <nav aria-label="Principal">
          <Link to="/pos">Caja</Link>
          <Link to="/ventas">Ventas</Link>
        </nav>
        <div>
          <span>{session.readOperatorName()}</span>
          <button
            type="button"
            onClick={() => {
              logout(session)
              navigate('/login')
            }}
          >
            Salir
          </button>
        </div>
      </header>
      <main>{children}</main>
    </div>
  )
}

function Protected({ children }: { children: ReactNode }) {
  return (
    <RequireAuth>
      <Shell>{children}</Shell>
    </RequireAuth>
  )
}

export default function App() {
  const { session } = useServices()
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/pos" element={<Protected><PosPage /></Protected>} />
      <Route path="/ventas" element={<Protected><SalesPage /></Protected>} />
      <Route path="/ventas/:id" element={<Protected><SaleDetailPage /></Protected>} />
      <Route path="*" element={<Navigate to={session.readToken() ? '/pos' : '/login'} replace />} />
    </Routes>
  )
}
