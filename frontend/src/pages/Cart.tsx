import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useCart } from '../context/CartContext'
import { api, errorMessage } from '../services/api'
import type { CartItem } from '../types/api'
import { cartIssueLabels, formatPrice } from '../utils/format'

export default function Cart() {
  const { cart, setCart, reloadCart } = useCart()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    reloadCart().catch(err => setError(errorMessage(err)))
  }, [reloadCart])

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError('')
    try {
      await action()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  const changeQuantity = (item: CartItem, quantity: number) =>
    run(async () => setCart(await api.setCartQuantity(item.productVariantId, quantity)))

  const removeItem = (item: CartItem) =>
    run(async () => {
      await api.removeFromCart(item.productVariantId)
      await reloadCart()
    })

  const clearCart = () =>
    run(async () => {
      await api.clearCart()
      await reloadCart()
    })

  if (!cart) return <p className="muted">Sepet yükleniyor…</p>

  if (cart.items.length === 0) {
    return (
      <div className="stack">
        <h1>Sepetim</h1>
        {error && <p className="alert error">{error}</p>}
        <div className="card empty">
          <p>Sepetiniz boş.</p>
          <Link to="/" className="button">Alışverişe başla</Link>
        </div>
      </div>
    )
  }

  return (
    <div className="stack">
      <div className="page-header">
        <h1>Sepetim</h1>
        <button className="button danger small" disabled={busy} onClick={clearCart}>Sepeti boşalt</button>
      </div>
      {error && <p className="alert error">{error}</p>}

      <div className="split">
        <div className="card">
          {cart.items.map(item => (
            <div key={item.productVariantId} className="cart-item">
              <div className="cart-item-info">
                {item.productId ? (
                  <Link to={`/products/${item.productId}`}><strong>{item.productName}</strong></Link>
                ) : (
                  <strong>Ürün bulunamadı</strong>
                )}
                <div className="muted small">
                  {item.options.map(option => `${option.name}: ${option.value}`).join(', ')}
                  {item.sku && <> · SKU: {item.sku}</>}
                </div>
                <div className="small">
                  {item.unitPrice !== null && <>Birim fiyat: {formatPrice(item.unitPrice, cart.currency)}</>}
                  {item.issue && <span className="text-danger"> · {cartIssueLabels[item.issue] ?? item.issue}</span>}
                  {item.issue === 'insufficient_stock' && <span className="muted"> (stokta {item.availableStock} adet)</span>}
                </div>
              </div>

              <div className="row">
                <button
                  className="button secondary small"
                  aria-label="Adedi azalt"
                  disabled={busy || item.quantity <= 1}
                  onClick={() => changeQuantity(item, item.quantity - 1)}
                >
                  −
                </button>
                <span aria-label="Adet">{item.quantity}</span>
                <button
                  className="button secondary small"
                  aria-label="Adedi artır"
                  disabled={busy || item.quantity >= Math.min(item.availableStock, 999)}
                  onClick={() => changeQuantity(item, item.quantity + 1)}
                >
                  +
                </button>
              </div>

              <strong>{item.lineTotal !== null ? formatPrice(item.lineTotal, cart.currency) : '—'}</strong>
              <button className="link-button small" disabled={busy} onClick={() => removeItem(item)}>Kaldır</button>
            </div>
          ))}
        </div>

        <div className="card stack">
          <h2>Sipariş özeti</h2>
          <div className="summary-row total">
            <span>Toplam</span>
            <span>{cart.totalAmount !== null ? formatPrice(cart.totalAmount, cart.currency) : '—'}</span>
          </div>
          <p className="muted small">
            Fiyatlar güncel katalogdan hesaplanır. Sepete eklemek stok ayırmaz; stok sipariş anında tekrar kontrol edilir.
          </p>
          {cart.canCheckout ? (
            <Link to="/checkout" className="button full">Siparişe geç</Link>
          ) : (
            <p className="alert warning small">Sipariş vermek için işaretli ürünlerin adedini azaltın veya ürünleri kaldırın.</p>
          )}
        </div>
      </div>
    </div>
  )
}
