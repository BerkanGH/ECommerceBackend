import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import Pagination from '../../components/Pagination'
import StatusBadge from '../../components/StatusBadge'
import { api, errorMessage } from '../../services/api'
import type { OrderStatus, OrderSummary, PagedResult } from '../../types/api'
import { formatDate, formatPrice, orderStatusLabels } from '../../utils/format'

export default function AdminOrders() {
  const [status, setStatus] = useState<OrderStatus | ''>('')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<OrderSummary> | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api.adminOrders(status || undefined, page).then(setResult).catch(err => setError(errorMessage(err)))
  }, [status, page])

  return (
    <div className="stack">
      <div className="page-header">
        <h1>Siparişler</h1>
        <label className="row">
          Durum
          <select value={status} onChange={e => {
            setStatus(e.target.value as OrderStatus | '')
            setPage(1)
          }}>
            <option value="">Tümü</option>
            {Object.entries(orderStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
      </div>
      {error && <p className="alert error">{error}</p>}

      <div className="card table-wrap">
        {!result && !error && <p className="muted">Yükleniyor…</p>}
        {result?.items.length === 0 && <p className="empty">Sipariş bulunamadı.</p>}
        {result && result.items.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Sipariş no</th>
                <th>Müşteri no</th>
                <th>Durum</th>
                <th className="align-right">Toplam</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map(order => (
                <tr key={order.id}>
                  <td>{formatDate(order.createdAtUtc)}</td>
                  <td><Link to={`/admin/orders/${order.id}`}>{order.id.slice(0, 8).toUpperCase()}</Link></td>
                  <td className="muted small">{order.userId.slice(0, 8).toUpperCase()}</td>
                  <td><StatusBadge status={order.status} /></td>
                  <td className="align-right">{formatPrice(order.totalAmount, order.currency)}</td>
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
