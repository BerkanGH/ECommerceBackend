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
        <Link to="/" className="brand">
          <span className="brand-mark" aria-hidden="true">E</span>
          <span className="brand-name">ECommerce</span>
        </Link>
        <nav className="nav" aria-label="Ana menü">
          <NavLink to="/" end>Mağaza</NavLink>
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
              <option value="TRY">₺ TRY</option>
              <option value="USD">$ USD</option>
            </select>
          )}
          {user ? (
            <>
              <NavLink to="/account" title={user.email}>
                Hesabım<span className="header-email muted small"> ({user.email})</span>
              </NavLink>
              {isCustomer && (
                <NavLink to="/cart" className="cart-link">
                  Sepet{itemCount > 0 && <span className="badge-count">{itemCount}</span>}
                </NavLink>
              )}
              <button className="link-button small" onClick={handleLogout}>Çıkış</button>
            </>
          ) : (
            <>
              <NavLink to="/login">Giriş yap</NavLink>
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
