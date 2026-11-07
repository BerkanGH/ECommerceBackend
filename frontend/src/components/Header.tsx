import { useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useCart } from '../context/CartContext'
import { errorMessage } from '../services/api'
import type { Currency } from '../types/api'

export default function Header() {
  const { user, isAdmin, isCustomer, logout } = useAuth()
  const { cart, currency, itemCount, changeCurrency } = useCart()
  const [currencyError, setCurrencyError] = useState('')
  const navigate = useNavigate()
  const cartHasItems = (cart?.items.length ?? 0) > 0

  async function handleCurrencyChange(value: Currency) {
    setCurrencyError('')
    try {
      await changeCurrency(value)
    } catch (error) {
      setCurrencyError(errorMessage(error))
    }
  }

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <header className="header">
      <div className="container header-inner">
        <Link to="/" className="brand">ECommerce</Link>
        <nav className="nav">
          <NavLink to="/" end>Ürünler</NavLink>
          {isCustomer && <NavLink to="/orders">Siparişlerim</NavLink>}
          {isAdmin && <NavLink to="/admin">Yönetim</NavLink>}
        </nav>
        <div className="header-actions">
          {!isAdmin && (
            <select
              aria-label="Para birimi"
              value={currency}
              disabled={cartHasItems}
              title={cartHasItems ? 'Sepette ürün varken para birimi değiştirilemez' : undefined}
              onChange={event => handleCurrencyChange(event.target.value as Currency)}
            >
              <option value="TRY">TRY ₺</option>
              <option value="USD">USD $</option>
            </select>
          )}
          {isCustomer && (
            <NavLink to="/cart">
              Sepet{itemCount > 0 && <span className="badge-count">{itemCount}</span>}
            </NavLink>
          )}
          {user ? (
            <>
              <span className="muted small">{user.email}</span>
              <button className="button secondary small" onClick={handleLogout}>Çıkış yap</button>
            </>
          ) : (
            <>
              <Link to="/login">Giriş yap</Link>
              <Link to="/register" className="button small">Kayıt ol</Link>
            </>
          )}
        </div>
      </div>
      {currencyError && (
        <div className="container">
          <p className="alert error small">{currencyError}</p>
        </div>
      )}
    </header>
  )
}
