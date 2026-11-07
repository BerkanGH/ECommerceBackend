import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useCart } from '../context/CartContext'
import { api, errorMessage } from '../services/api'
import type { Product } from '../types/api'
import { formatPrice } from '../utils/format'

function initialSelection(product: Product): Record<string, string> {
  const variant = product.variants.find(item => item.stockQuantity > 0) ?? product.variants[0]
  const selection: Record<string, string> = {}
  for (const option of product.options) {
    const value = option.values.find(item => variant?.optionValueIds.includes(item.id)) ?? option.values[0]
    if (value) selection[option.id] = value.id
  }
  return selection
}

export default function ProductDetail() {
  const { id = '' } = useParams()
  const { user, isAdmin } = useAuth()
  const { currency, setCart } = useCart()
  const navigate = useNavigate()
  const location = useLocation()

  const [product, setProduct] = useState<Product | null>(null)
  const [selection, setSelection] = useState<Record<string, string>>({})
  const [quantity, setQuantity] = useState(1)
  const [loadError, setLoadError] = useState('')
  const [error, setError] = useState('')
  const [added, setAdded] = useState(false)
  const [adding, setAdding] = useState(false)

  useEffect(() => {
    api.product(id)
      .then(data => {
        setProduct(data)
        setSelection(initialSelection(data))
      })
      .catch(err => setLoadError(errorMessage(err)))
  }, [id])

  if (loadError) {
    return (
      <div className="card empty">
        <p>{loadError}</p>
        <Link to="/">Ürünlere dön</Link>
      </div>
    )
  }
  if (!product) return <p className="muted">Ürün yükleniyor…</p>

  const variant = product.variants.find(item =>
    product.options.every(option => item.optionValueIds.includes(selection[option.id])))
  const price = variant?.prices.find(item => item.currency === currency)
  const stock = variant?.stockQuantity ?? 0
  const maxQuantity = Math.min(stock, 999)
  const canBuy = variant !== undefined && price !== undefined && stock > 0

  function selectValue(optionId: string, valueId: string) {
    setSelection(current => ({ ...current, [optionId]: valueId }))
    setQuantity(1)
    setAdded(false)
    setError('')
  }

  async function addToCart() {
    if (!variant) return
    if (!user) {
      navigate('/login', { state: { from: location.pathname } })
      return
    }
    setAdding(true)
    setError('')
    setAdded(false)
    try {
      setCart(await api.addToCart(variant.id, quantity, currency))
      setAdded(true)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setAdding(false)
    }
  }

  return (
    <div className="stack">
      <Link to="/" className="small">← Ürünlere dön</Link>
      <div className="product-detail">
        <div className="product-image large" aria-hidden="true">{product.name.charAt(0).toUpperCase()}</div>

        <div className="stack">
          <div>
            <h1>{product.name}</h1>
            {product.description && <p className="muted">{product.description}</p>}
          </div>

          {product.options.map(option => (
            <div key={option.id} className="option-group">
              <strong className="small">{option.name}</strong>
              <div className="option-values">
                {option.values.map(value => (
                  <button
                    key={value.id}
                    type="button"
                    className={selection[option.id] === value.id ? 'selected' : ''}
                    onClick={() => selectValue(option.id, value.id)}
                  >
                    {value.value}
                  </button>
                ))}
              </div>
            </div>
          ))}

          {product.variants.length === 0 && <p className="alert warning">Bu ürün henüz satışa hazır değil.</p>}
          {product.variants.length > 0 && !variant && (
            <p className="alert warning">Bu seçim kombinasyonu mevcut değil. Başka bir seçenek deneyin.</p>
          )}

          {variant && (
            <div className="stack">
              <div className="price-large">
                {price ? formatPrice(price.amount, currency) : `Bu ürünün ${currency} fiyatı yok.`}
              </div>
              <div className="small">
                {stock > 0 ? <span>Stokta {stock} adet var</span> : <strong className="text-danger">Tükendi</strong>}
                <span className="muted"> · SKU: {variant.sku}</span>
              </div>

              {isAdmin ? (
                <p className="alert info">Yönetici hesabıyla alışveriş yapılamaz.</p>
              ) : (
                <div className="row">
                  <label className="row">
                    Adet
                    <input
                      type="number"
                      className="quantity-input"
                      min={1}
                      max={Math.max(maxQuantity, 1)}
                      value={quantity}
                      disabled={!canBuy}
                      onChange={event => setQuantity(Math.min(Math.max(1, Number(event.target.value) || 1), Math.max(maxQuantity, 1)))}
                    />
                  </label>
                  <button className="button" disabled={!canBuy || adding} onClick={addToCart}>
                    {adding ? 'Ekleniyor…' : 'Sepete ekle'}
                  </button>
                </div>
              )}
              {!user && <p className="muted small">Sepete eklemek için giriş yapmanız gerekir.</p>}
            </div>
          )}

          {added && (
            <p className="alert success">
              Ürün sepete eklendi. <Link to="/cart">Sepete git</Link>
            </p>
          )}
          {error && <p className="alert error">{error}</p>}
        </div>
      </div>
    </div>
  )
}
