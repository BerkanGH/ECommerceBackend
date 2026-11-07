import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api, errorMessage } from '../services/api'

export default function Account() {
  const { user, isAdmin, isCustomer, logout } = useAuth()
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  if (!user) return null

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await api.changePassword(currentPassword, newPassword)
      // Changing the password invalidates refresh tokens, so the user signs in again.
      await logout()
      navigate('/login', { replace: true, state: { passwordChanged: true } })
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="stack">
      <h1>Hesabım</h1>
      <div className="split">
        <form className="card form" onSubmit={handleSubmit}>
          <h2>Şifre değiştir</h2>
          <label>
            Mevcut şifre
            <input type="password" autoComplete="current-password" required value={currentPassword}
              onChange={e => setCurrentPassword(e.target.value)} />
          </label>
          <label>
            Yeni şifre
            <input type="password" autoComplete="new-password" required minLength={10} maxLength={128} value={newPassword}
              onChange={e => setNewPassword(e.target.value)} />
            <span className="field-hint">En az 10 karakter; büyük harf, küçük harf, rakam ve özel karakter içermeli.</span>
          </label>
          {error && <p className="alert error">{error}</p>}
          <p className="muted small">Şifreniz değişince tüm oturumlarınız kapanır ve yeniden giriş yapmanız gerekir.</p>
          <div>
            <button className="button" disabled={saving}>{saving ? 'Kaydediliyor…' : 'Şifreyi değiştir'}</button>
          </div>
        </form>

        <div className="card stack">
          <h2>Hesap bilgileri</h2>
          <div className="summary-row">
            <span className="muted">E-posta</span>
            <span>{user.email}</span>
          </div>
          <div className="summary-row">
            <span className="muted">Hesap türü</span>
            <span>{isAdmin ? 'Yönetici' : 'Müşteri'}</span>
          </div>
          {isCustomer && <Link to="/orders" className="button secondary">Siparişlerim</Link>}
          {isAdmin && <Link to="/admin" className="button secondary">Yönetim paneli</Link>}
        </div>
      </div>
    </div>
  )
}
