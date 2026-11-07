import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { ApiError, errorMessage } from '../services/api'

export default function Login() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as { from?: string; passwordChanged?: boolean } | null
  const from = state?.from ?? '/'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to={from} replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      await login(email.trim(), password)
      navigate(from, { replace: true })
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401
        ? 'E-posta veya şifre hatalı. Çok sayıda hatalı denemeden sonra hesap 5 dakika kilitlenir.'
        : errorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="card auth-card">
      <h1>Giriş yap</h1>
      <p className="muted">Sepetinize ve siparişlerinize ulaşmak için giriş yapın.</p>
      {state?.passwordChanged && <p className="alert success">Şifreniz değişti. Yeni şifrenizle giriş yapın.</p>}
      <form className="form" onSubmit={handleSubmit}>
        <label>
          E-posta
          <input type="email" autoComplete="email" required value={email} onChange={e => setEmail(e.target.value)} />
        </label>
        <label>
          Şifre
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={e => setPassword(e.target.value)}
          />
        </label>
        {error && <p className="alert error">{error}</p>}
        <button className="button full" disabled={submitting}>{submitting ? 'Giriş yapılıyor…' : 'Giriş yap'}</button>
      </form>
      <p className="muted small auth-switch">
        Hesabınız yok mu? <Link to="/register" state={location.state}>Kayıt olun</Link>
      </p>
    </div>
  )
}
