import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

interface Props {
  role?: 'Customer' | 'Admin'
  children: ReactNode
}

// Only hides pages in the UI; the API enforces the real role checks.
export default function ProtectedRoute({ role, children }: Props) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return <p className="muted">Yükleniyor…</p>
  if (!user) return <Navigate to="/login" state={{ from: location.pathname }} replace />
  if (role && !user.roles.includes(role)) {
    return (
      <div className="card empty">
        <h2>Bu sayfaya erişiminiz yok</h2>
        <p>
          {role === 'Admin'
            ? 'Bu sayfa sadece yönetici hesapları içindir.'
            : 'Alışveriş yapmak için müşteri hesabıyla giriş yapın.'}
        </p>
      </div>
    )
  }
  return children
}
