import { Link } from 'react-router-dom'
import type { Currency, ProductSummary } from '../types/api'
import { formatPrice } from '../utils/format'

interface Props {
  product: ProductSummary
  currency: Currency
}

export default function ProductCard({ product, currency }: Props) {
  const price = currency === 'TRY' ? product.minPriceTry : product.minPriceUsd

  return (
    <Link to={`/products/${product.id}`} className="product-card">
      <div className="product-image" aria-hidden="true">{product.name.charAt(0).toUpperCase()}</div>
      <div className="product-card-body">
        <span className="name">{product.name}</span>
        {product.description && <span className="muted small">{product.description}</span>}
        <span className="price">
          {price === null ? <span className="muted small">{currency} fiyatı yok</span> : formatPrice(price, currency)}
        </span>
        {!product.inStock && <span className="small text-danger">Tükendi</span>}
      </div>
    </Link>
  )
}
