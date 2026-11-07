import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import Pagination from '../components/Pagination'
import StatusBadge from '../components/StatusBadge'
import { api, errorMessage } from '../services/api'
import type { OrderSummary, PagedResult } from '../types/api'
import { formatDate, formatPrice } from '../utils/format'

export default function Orders() {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<OrderSummary> | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api.orders(page).then(setResult).catch(err => setError(errorMessage(err)))
  }, [page])

  return (
    <div className="stack">
      <h1>Siparişlerim</h1>
      {error && <p className="alert error">{error}</p>}
      {!result && !error && <p className="muted">Yükleniyor…</p>}
      {result && result.items.length === 0 && (
        <div className="card empty">
          <p>Henüz siparişiniz yok.</p>
          <Link to="/" className="button">Alışverişe başla</Link>
        </div>
      )}
      {result && result.items.length > 0 && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Sipariş no</th>
                <th>Durum</th>
                <th className="align-right">Toplam</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map(order => (
                <tr key={order.id}>
                  <td>{formatDate(order.createdAtUtc)}</td>
                  <td><Link to={`/orders/${order.id}`}>{order.id.slice(0, 8).toUpperCase()}</Link></td>
                  <td><StatusBadge status={order.status} /></td>
                  <td className="align-right">{formatPrice(order.totalAmount, order.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {result && (
        <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount} onChange={setPage} />
      )}
    </div>
  )
}
