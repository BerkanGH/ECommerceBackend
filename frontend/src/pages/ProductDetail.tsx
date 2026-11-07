import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import ProductImage from '../components/ProductImage'
import { useAuth } from '../context/AuthContext'
import { useCart } from '../context/CartContext'
import { api, errorMessage } from '../services/api'
import type { Category, Product } from '../types/api'
import { formatPrice, sortOptionValues } from '../utils/format'

type Selection = Record<string, string>

function findVariant(product: Product, selection: Selection) {
  return product.variants.find(variant =>
    product.options.every(option => variant.optionValueIds.includes(selection[option.id])))
}

// Picks the first in-stock combination, following the order the values are shown in.
function initialSelection(product: Product): Selection {
  const selection: Selection = {}
  for (const option of product.options) {
    const values = sortOptionValues(option.values)
    const inStock = values.find(value => product.variants.some(variant =>
      variant.stockQuantity > 0 &&
      [...Object.values(selection), value.id].every(id => variant.optionValueIds.includes(id))))
    const value = inStock ?? values[0]
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
  const [category, setCategory] = useState<Category | null>(null)
  const [selection, setSelection] = useState<Selection>({})
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
        return api.categories().then(result =>
          setCategory(result.items.find(item => item.id === data.categoryId) ?? null))
      })
      .catch(err => setLoadError(errorMessage(err)))
  }, [id])

  if (loadError) {
    return (
      <div className="card empty">
        <p>{loadError}</p>
        <Link to="/" className="button secondary">Mağazaya dön</Link>
      </div>
    )
  }
  if (!product) return <p className="muted">Ürün yükleniyor…</p>

  const variant = findVariant(product, selection)
  const price = variant?.prices.find(item => item.currency === currency)
  const stock = variant?.stockQuantity ?? 0
  const maxQuantity = Math.max(Math.min(stock, 999), 1)
  const canBuy = variant !== undefined && price !== undefined && stock > 0

  function isAvailable(optionId: string, valueId: string) {
    const candidate = findVariant(product!, { ...selection, [optionId]: valueId })
    return candidate !== undefined && candidate.stockQuantity > 0
  }

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
      <nav className="breadcrumb" aria-label="Konum">
        <Link to="/">Mağaza</Link>
        {category && (
          <>
            <span>/</span>
            <Link to={`/?category=${category.id}`}>{category.name}</Link>
          </>
        )}
        <span>/</span>
        <span>{product.name}</span>
      </nav>

      <div className="product-detail">
        <ProductImage src={product.imageUrl} alt={product.name} className="large" />

        <div className="product-info">
          <div>
            <h1>{product.name}</h1>
            {product.description && <p className="muted">{product.description}</p>}
          </div>

          {variant && (
            <div className="price-large">
              {price ? formatPrice(price.amount, currency) : <span className="muted small">Bu ürünün {currency} fiyatı yok.</span>}
            </div>
          )}

          {product.options.map(option => (
            <div key={option.id} className="option-group">
              <strong className="small">{option.name}</strong>
              <div className="option-values">
                {sortOptionValues(option.values).map(value => {
                  const classes = [
                    selection[option.id] === value.id ? 'selected' : '',
                    isAvailable(option.id, value.id) ? '' : 'unavailable',
                  ].join(' ')
                  return (
                    <button key={value.id} type="button" className={classes} onClick={() => selectValue(option.id, value.id)}>
                      {value.value}
                    </button>
                  )
                })}
              </div>
            </div>
          ))}

          {product.variants.length === 0 && <p className="alert warning">Bu ürün henüz satışa hazır değil.</p>}
          {product.variants.length > 0 && !variant && (
            <p className="alert warning">Bu seçim kombinasyonu mevcut değil. Başka bir seçenek deneyin.</p>
          )}

          {variant && (
            <>
              <div className="stock-line">
                {stock > 0
                  ? <span>{stock <= 5 ? `Son ${stock} ürün` : 'Stokta var'}</span>
                  : <strong className="text-danger">Bu seçenek tükendi</strong>}
                <span className="muted small"> · SKU: {variant.sku}</span>
              </div>

              {isAdmin ? (
                <p className="alert info">Yönetici hesabıyla alışveriş yapılamaz.</p>
              ) : (
                <div className="buy-box">
                  <input
                    type="number"
                    className="quantity-input"
                    aria-label="Adet"
                    min={1}
                    max={maxQuantity}
                    value={quantity}
                    disabled={!canBuy}
                    onChange={event => setQuantity(Math.min(Math.max(1, Number(event.target.value) || 1), maxQuantity))}
                  />
                  <button className="button large" disabled={!canBuy || adding} onClick={addToCart}>
                    {adding ? 'Ekleniyor…' : 'Sepete ekle'}
                  </button>
                </div>
              )}
            </>
          )}

          {added && (
            <p className="alert success">
              Ürün sepete eklendi. <Link to="/cart">Sepete git</Link>
            </p>
          )}
          {error && <p className="alert error">{error}</p>}

          <div className="info-list">
            {!user && <span>Sepete eklemek için giriş yapmanız gerekir.</span>}
            <span>Fiyat ve stok sipariş anında sunucuda tekrar kontrol edilir.</span>
            <span>Demo mağaza: sipariş verildiğinde ödeme alınmaz.</span>
          </div>
        </div>
      </div>
    </div>
  )
}
