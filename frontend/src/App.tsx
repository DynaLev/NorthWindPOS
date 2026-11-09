import type { ReactNode } from 'react'
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { clearToken, readOperatorName, readToken } from './auth/session'
import { LoginPage } from './pages/LoginPage'
import { PosPage } from './pages/PosPage'
import { SaleDetailPage } from './pages/SaleDetailPage'
import { SalesPage } from './pages/SalesPage'

function RequireAuth({ children }: { children: ReactNode }) {
  if (!readToken()) return <Navigate to="/login" replace />
  return <>{children}</>
}

function Shell({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  return (
    <div className="pos-shell">
      <header>
        <div>
          <p className="eyebrow">NorthWind</p>
          <strong>Punto de venta</strong>
        </div>
        <nav>
          <Link to="/pos">Caja</Link>
          <Link to="/ventas">Ventas</Link>
        </nav>
        <div>
          <span>{readOperatorName()}</span>
          <button
            type="button"
            onClick={() => {
              clearToken()
              navigate('/login')
            }}
          >
            Salir
          </button>
        </div>
      </header>
      {children}
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/pos"
        element={
          <RequireAuth>
            <Shell>
              <PosPage />
            </Shell>
          </RequireAuth>
        }
      />
      <Route
        path="/ventas"
        element={
          <RequireAuth>
            <Shell>
              <SalesPage />
            </Shell>
          </RequireAuth>
        }
      />
      <Route
        path="/ventas/:id"
        element={
          <RequireAuth>
            <Shell>
              <SaleDetailPage />
            </Shell>
          </RequireAuth>
        }
      />
      <Route path="*" element={<Navigate to={readToken() ? '/pos' : '/login'} replace />} />
    </Routes>
  )
}
