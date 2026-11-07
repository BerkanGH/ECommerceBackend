import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import OrderDetails from '../../components/OrderDetails'
import { api, errorMessage, type OrderAction } from '../../services/api'
import type { Order, OrderStatus } from '../../types/api'

const actionsByStatus: Record<OrderStatus, { action: OrderAction; label: string }[]> = {
  Pending: [{ action: 'confirm', label: 'Onayla' }, { action: 'cancel', label: 'İptal et' }],
  Confirmed: [{ action: 'ship', label: 'Kargoya verildi' }, { action: 'cancel', label: 'İptal et' }],
  Shipped: [{ action: 'deliver', label: 'Teslim edildi' }],
  Delivered: [],
  Cancelled: [],
}

export default function AdminOrderDetail() {
  const { id = '' } = useParams()
  const [order, setOrder] = useState<Order | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api.adminOrder(id).then(setOrder).catch(err => setError(errorMessage(err)))
  }, [id])

  async function changeStatus(action: OrderAction) {
    if (action === 'cancel' && !window.confirm('Sipariş iptal edilecek ve stok iade edilecek. Devam edilsin mi?')) return
    setBusy(true)
    setError('')
    try {
      setOrder(await api.changeOrderStatus(id, action))
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  if (!order) return error ? <p className="alert error">{error}</p> : <p className="muted">Sipariş yükleniyor…</p>

  return (
    <div className="stack">
      <Link to="/admin/orders" className="small">← Siparişler</Link>
      <div className="page-header">
        <h1>Sipariş {order.id.slice(0, 8).toUpperCase()}</h1>
        <div className="row">
          {actionsByStatus[order.status].map(({ action, label }) => (
            <button key={action} className={action === 'cancel' ? 'button danger' : 'button'} disabled={busy}
              onClick={() => changeStatus(action)}>
              {label}
            </button>
          ))}
        </div>
      </div>
      <p className="muted small">Müşteri no: {order.userId}. Durumlar elle güncellenir; gerçek ödeme veya kargo entegrasyonu yoktur.</p>
      {error && <p className="alert error">{error}</p>}
      <OrderDetails order={order} />
    </div>
  )
}
