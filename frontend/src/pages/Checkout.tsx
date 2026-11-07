import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useCart } from '../context/CartContext'
import { ApiError, api, errorMessage } from '../services/api'
import type { Address } from '../types/api'
import { formatPrice } from '../utils/format'

const emptyAddress: Address = {
  recipientName: '',
  phone: '',
  countryCode: 'TR',
  city: '',
  postalCode: '',
  addressLine: '',
}

export default function Checkout() {
  const { cart, reloadCart } = useCart()
  const navigate = useNavigate()
  const [address, setAddress] = useState<Address>(emptyAddress)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  // One key per checkout attempt: retrying after a network error reuses it,
  // so the server returns the existing order instead of creating a second one.
  const idempotencyKey = useRef(crypto.randomUUID())

  useEffect(() => {
    reloadCart().catch(err => setError(errorMessage(err)))
  }, [reloadCart])

  function update(field: keyof Address, value: string) {
    setAddress(current => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!cart || cart.totalAmount === null) return
    setSubmitting(true)
    setError('')
    try {
      const order = await api.checkout(idempotencyKey.current, {
        expectedCurrency: cart.currency,
        expectedTotal: cart.totalAmount,
        shippingAddress: { ...address, countryCode: address.countryCode.toUpperCase() },
      })
      await reloadCart()
      navigate(`/orders/${order.id}`, { state: { created: true } })
    } catch (err) {
      setError(errorMessage(err))
      if (err instanceof ApiError && err.status === 409) await reloadCart().catch(() => undefined)
    } finally {
      setSubmitting(false)
    }
  }

  if (!cart) return <p className="muted">Yükleniyor…</p>
  if (cart.items.length === 0) {
    return (
      <div className="card empty">
        <p>Sepetiniz boş olduğu için sipariş oluşturulamaz.</p>
        <Link to="/" className="button">Ürünlere göz at</Link>
      </div>
    )
  }

  return (
    <div className="stack">
      <h1>Siparişi tamamla</h1>
      <p className="alert info">
        Bu bir portföy projesidir: sipariş oluşturduğunuzda ödeme alınmaz ve ürün gönderilmez.
      </p>

      <div className="split">
        <form id="checkout-form" className="card form" onSubmit={handleSubmit}>
          <h2>Teslimat adresi</h2>
          <div className="form-grid">
            <label>
              Ad soyad
              <input required maxLength={100} autoComplete="name" value={address.recipientName}
                onChange={e => update('recipientName', e.target.value)} />
            </label>
            <label>
              Telefon
              <input required maxLength={30} type="tel" autoComplete="tel" value={address.phone}
                onChange={e => update('phone', e.target.value)} />
            </label>
            <label className="full">
              Adres
              <textarea required maxLength={500} autoComplete="street-address" value={address.addressLine}
                onChange={e => update('addressLine', e.target.value)} />
            </label>
            <label>
              Şehir
              <input required maxLength={100} autoComplete="address-level2" value={address.city}
                onChange={e => update('city', e.target.value)} />
            </label>
            <label>
              Posta kodu
              <input required maxLength={20} autoComplete="postal-code" value={address.postalCode}
                onChange={e => update('postalCode', e.target.value)} />
            </label>
            <label>
              Ülke kodu
              <input required minLength={2} maxLength={2} pattern="[A-Za-z]{2}" title="İki harfli ülke kodu, ör. TR"
                autoComplete="country" value={address.countryCode} onChange={e => update('countryCode', e.target.value)} />
              <span className="field-hint">İki harf, ör. TR</span>
            </label>
          </div>
        </form>

        <div className="card stack">
          <h2>Sipariş özeti</h2>
          {cart.items.map(item => (
            <div key={item.productVariantId} className="summary-row small">
              <span>{item.productName} × {item.quantity}</span>
              <span>{item.lineTotal !== null ? formatPrice(item.lineTotal, cart.currency) : '—'}</span>
            </div>
          ))}
          <div className="summary-row total">
            <span>Toplam</span>
            <span>{cart.totalAmount !== null ? formatPrice(cart.totalAmount, cart.currency) : '—'}</span>
          </div>
          {error && <p className="alert error small">{error}</p>}
          {!cart.canCheckout && (
            <p className="alert warning small">
              Sepetinizde sorunlu ürünler var. <Link to="/cart">Sepete dönün</Link>
            </p>
          )}
          <button form="checkout-form" className="button full" disabled={submitting || !cart.canCheckout}>
            {submitting ? 'Sipariş oluşturuluyor…' : 'Siparişi onayla'}
          </button>
          <p className="muted small">Tutar sunucuda tekrar hesaplanır. Fiyat değiştiyse güncel tutarı onaylamanız istenir.</p>
        </div>
      </div>
    </div>
  )
}
