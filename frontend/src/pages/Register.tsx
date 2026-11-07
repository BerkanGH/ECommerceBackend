import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { errorMessage } from '../services/api'

function passwordProblem(password: string) {
  if (password.length < 10) return 'Şifre en az 10 karakter olmalı.'
  if (!/[a-z]/.test(password) || !/[A-Z]/.test(password)) return 'Şifre büyük ve küçük harf içermeli.'
  if (!/\d/.test(password)) return 'Şifre en az bir rakam içermeli.'
  if (!/[^a-zA-Z0-9]/.test(password)) return 'Şifre en az bir özel karakter içermeli (ör. ! veya *).'
  return ''
}

export default function Register() {
  const { user, register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [passwordAgain, setPasswordAgain] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to={from} replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const problem = password !== passwordAgain ? 'Şifreler eşleşmiyor.' : passwordProblem(password)
    if (problem) {
      setError(problem)
      return
    }
    setSubmitting(true)
    setError('')
    try {
      await register(email.trim(), password)
      navigate(from, { replace: true })
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="card auth-card">
      <h1>Kayıt ol</h1>
      <p className="muted">Birkaç saniyede müşteri hesabınızı oluşturun.</p>
      <form className="form" onSubmit={handleSubmit}>
        <label>
          E-posta
          <input type="email" autoComplete="email" required maxLength={256} value={email} onChange={e => setEmail(e.target.value)} />
        </label>
        <label>
          Şifre
          <input
            type="password"
            autoComplete="new-password"
            required
            maxLength={128}
            value={password}
            onChange={e => setPassword(e.target.value)}
          />
          <span className="field-hint">En az 10 karakter; büyük harf, küçük harf, rakam ve özel karakter içermeli.</span>
        </label>
        <label>
          Şifre (tekrar)
          <input
            type="password"
            autoComplete="new-password"
            required
            maxLength={128}
            value={passwordAgain}
            onChange={e => setPasswordAgain(e.target.value)}
          />
        </label>
        {error && <p className="alert error">{error}</p>}
        <button className="button full" disabled={submitting}>{submitting ? 'Hesap oluşturuluyor…' : 'Kayıt ol'}</button>
      </form>
      <p className="muted small auth-switch">
        Zaten hesabınız var mı? <Link to="/login" state={location.state}>Giriş yapın</Link>
      </p>
    </div>
  )
}
