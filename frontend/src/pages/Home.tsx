import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import Pagination from '../components/Pagination'
import ProductCard from '../components/ProductCard'
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

  const selectedCategory = categories.find(category => category.id === categoryId)

  return (
    <div className="catalog">
      <aside>
        <h3>Kategoriler</h3>
        <div className="category-list">
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
      </aside>

      <section>
        <div className="page-header">
          <h1>{selectedCategory?.name ?? 'Tüm ürünler'}</h1>
          <form className="row" onSubmit={handleSearch} role="search">
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

        {error && <p className="alert error">{error}</p>}
        {!result && !error && <p className="muted">Ürünler yükleniyor…</p>}
        {result && result.items.length === 0 && (
          <div className="card empty">
            {search ? `"${search}" için ürün bulunamadı.` : 'Bu kategoride henüz ürün yok.'}
          </div>
        )}
        {result && result.items.length > 0 && (
          <>
            <div className="product-grid">
              {result.items.map(product => (
                <ProductCard key={product.id} product={product} currency={currency} />
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
