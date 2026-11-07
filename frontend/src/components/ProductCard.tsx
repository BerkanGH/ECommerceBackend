import { Link } from 'react-router-dom'
import type { Currency, ProductSummary } from '../types/api'
import { formatPrice } from '../utils/format'
import ProductImage from './ProductImage'

interface Props {
  product: ProductSummary
  currency: Currency
  categoryName?: string
}

export default function ProductCard({ product, currency, categoryName }: Props) {
  const price = currency === 'TRY' ? product.minPriceTry : product.minPriceUsd

  return (
    <Link to={`/products/${product.id}`} className="product-card">
      <ProductImage src={product.imageUrl} alt={product.name}>
        {!product.inStock && <span className="image-badge">Tükendi</span>}
      </ProductImage>
      <div className="product-card-body">
        {categoryName && <span className="category">{categoryName}</span>}
        <span className="name">{product.name}</span>
        <span className="price">
          {price === null ? <span className="muted small">{currency} fiyatı yok</span> : formatPrice(price, currency)}
        </span>
      </div>
    </Link>
  )
}

export function ProductCardSkeleton() {
  return (
    <div aria-hidden="true">
      <div className="skeleton image" />
      <div className="skeleton line" />
      <div className="skeleton line short" />
    </div>
  )
}
