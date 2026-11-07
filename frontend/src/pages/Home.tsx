import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import Pagination from '../components/Pagination'
import ProductCard, { ProductCardSkeleton } from '../components/ProductCard'
import ProductImage from '../components/ProductImage'
import { useCart } from '../context/CartContext'
import { api, errorMessage } from '../services/api'
import type { Category, PagedResult, ProductSummary } from '../types/api'

const pageSize = 12

export default function Home() {
  const { currency } = useCart()
  const [params, setParams] = useSearchParams()
  const categoryId = params.get('category') ?? ''
  const search = params.get('search') ?? ''
  const page = Number(params.get('page') ?? 1)

  const [categories, setCategories] = useState<Category[]>([])
  const [result, setResult] = useState<PagedResult<ProductSummary> | null>(null)
  const [searchText, setSearchText] = useState(search)
  const [error, setError] = useState('')

  useEffect(() => {
    api.categories().then(data => setCategories(data.items)).catch(() => setCategories([]))
  }, [])

  useEffect(() => {
    let ignore = false
    setResult(null)
    api.products({ categoryId: categoryId || undefined, search: search || undefined, page, pageSize })
      .then(data => {
        if (ignore) return
        setResult(data)
        setError('')
      })
      .catch(err => !ignore && setError(errorMessage(err)))
    return () => {
      ignore = true
    }
  }, [categoryId, search, page])

  function updateParams(changes: Record<string, string>) {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    if (!('page' in changes)) next.delete('page')
    setParams(next)
  }

  function handleSearch(event: FormEvent) {
    event.preventDefault()
    updateParams({ search: searchText.trim() })
  }

  const categoryNames = new Map(categories.map(category => [category.id, category.name]))
  const selectedCategory = categories.find(category => category.id === categoryId)
  const showHero = !categoryId && !search && page === 1
  const heroProducts = (result?.items ?? []).filter(product => product.imageUrl).slice(0, 3)

  return (
    <div>
      {showHero && (
        <section className="hero">
          <div>
            <h1>Günlük hayat için seçilmiş ürünler</h1>
            <p>Giyimden ev eşyalarına kadar sade ve kullanışlı ürünleri keşfedin.</p>
            <p className="small">Bu bir portföy demosudur: sipariş verebilirsiniz ama ödeme alınmaz ve ürün gönderilmez.</p>
            <a href="#products" className="button large">Alışverişe başla</a>
          </div>
          {heroProducts.length === 3 && (
            <div className="hero-images" aria-hidden="true">
              {heroProducts.map(product => (
                <ProductImage key={product.id} src={product.imageUrl} alt="" />
              ))}
            </div>
          )}
        </section>
      )}

      <section id="products">
        <div className="section-header">
          <div>
            <h2>{search ? `"${search}" için sonuçlar` : selectedCategory?.name ?? 'Tüm ürünler'}</h2>
            {result && <span className="muted small">{result.totalCount} ürün</span>}
          </div>
          <form className="search-form" onSubmit={handleSearch} role="search">
            <input
              type="search"
              placeholder="Ürün ara"
              aria-label="Ürün ara"
              maxLength={100}
              value={searchText}
              onChange={event => setSearchText(event.target.value)}
            />
            <button className="button secondary">Ara</button>
          </form>
        </div>

        <div className="chips" role="group" aria-label="Kategoriler">
          <button className={categoryId === '' ? 'active' : ''} onClick={() => updateParams({ category: '' })}>
            Tümü
          </button>
          {categories.map(category => (
            <button
              key={category.id}
              className={category.id === categoryId ? 'active' : ''}
              onClick={() => updateParams({ category: category.id })}
            >
              {category.name}
            </button>
          ))}
        </div>

        {error && <p className="alert error">{error}</p>}
        {!result && !error && (
          <div className="product-grid">
            {Array.from({ length: 8 }, (_, index) => <ProductCardSkeleton key={index} />)}
          </div>
        )}
        {result && result.items.length === 0 && (
          <div className="card empty">
            <p>{search ? 'Aramanızla eşleşen ürün bulunamadı.' : 'Bu kategoride henüz ürün yok.'}</p>
            {(search || categoryId) && (
              <button className="button secondary" onClick={() => {
                setSearchText('')
                setParams(new URLSearchParams())
              }}>
                Tüm ürünleri göster
              </button>
            )}
          </div>
        )}
        {result && result.items.length > 0 && (
          <>
            <div className="product-grid">
              {result.items.map(product => (
                <ProductCard
                  key={product.id}
                  product={product}
                  currency={currency}
                  categoryName={categoryNames.get(product.categoryId)}
                />
              ))}
            </div>
            <Pagination
              page={result.page}
              pageSize={result.pageSize}
              totalCount={result.totalCount}
              onChange={next => updateParams({ page: String(next) })}
            />
          </>
        )}
      </section>
    </div>
  )
}
