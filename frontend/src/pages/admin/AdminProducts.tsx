import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import Pagination from '../../components/Pagination'
import ProductImage from '../../components/ProductImage'
import { api, errorMessage } from '../../services/api'
import type { PagedResult, ProductSummary } from '../../types/api'
import { formatPrice } from '../../utils/format'

export default function AdminProducts() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [searchText, setSearchText] = useState('')
  const [result, setResult] = useState<PagedResult<ProductSummary> | null>(null)
  const [error, setError] = useState('')
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    api.adminProducts({ search: search || undefined, page, pageSize: 20 })
      .then(setResult)
      .catch(err => setError(errorMessage(err)))
  }, [search, page, reloadToken])

  function handleSearch(event: FormEvent) {
    event.preventDefault()
    setPage(1)
    setSearch(searchText.trim())
  }

  async function toggleAvailability(product: ProductSummary) {
    setError('')
    try {
      await api.setProductAvailability(product.id, !product.isActive)
      setReloadToken(token => token + 1)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="stack">
      <div className="page-header">
        <h1>Ürünler</h1>
        <div className="row">
          <form className="row" onSubmit={handleSearch} role="search">
            <input type="search" placeholder="Ürün ara" aria-label="Ürün ara" maxLength={100}
              value={searchText} onChange={e => setSearchText(e.target.value)} />
            <button className="button secondary">Ara</button>
          </form>
          <Link to="/admin/products/new" className="button">Yeni ürün</Link>
        </div>
      </div>
      {error && <p className="alert error">{error}</p>}

      <div className="card table-wrap">
        {!result && <p className="muted">Yükleniyor…</p>}
        {result?.items.length === 0 && <p className="empty">Ürün bulunamadı.</p>}
        {result && result.items.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Ürün</th>
                <th>Durum</th>
                <th>Stok</th>
                <th className="align-right">En düşük fiyat</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {result.items.map(product => (
                <tr key={product.id}>
                  <td>
                    <Link to={`/admin/products/${product.id}`} className="cell-product">
                      <ProductImage src={product.imageUrl} alt="" className="thumb" />
                      {product.name}
                    </Link>
                  </td>
                  <td>{product.isActive ? 'Satışta' : <span className="text-danger">Satışa kapalı</span>}</td>
                  <td>{product.inStock ? 'Var' : <span className="text-danger">Yok</span>}</td>
                  <td className="align-right small">
                    {product.minPriceTry !== null && <div>{formatPrice(product.minPriceTry, 'TRY')}</div>}
                    {product.minPriceUsd !== null && <div>{formatPrice(product.minPriceUsd, 'USD')}</div>}
                    {product.minPriceTry === null && product.minPriceUsd === null && <span className="muted">—</span>}
                  </td>
                  <td className="align-right">
                    <button className="button secondary small" onClick={() => toggleAvailability(product)}>
                      {product.isActive ? 'Satışa kapat' : 'Satışa aç'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
      {result && (
        <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount} onChange={setPage} />
      )}
    </div>
  )
}
