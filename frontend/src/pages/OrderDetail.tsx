import { useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import OrderDetails from '../components/OrderDetails'
import { api, errorMessage } from '../services/api'
import type { Order } from '../types/api'
import { canCancel } from '../utils/format'

export default function OrderDetail() {
  const { id = '' } = useParams()
  const location = useLocation()
  const justCreated = (location.state as { created?: boolean } | null)?.created === true

  const [order, setOrder] = useState<Order | null>(null)
  const [loadError, setLoadError] = useState('')
  const [error, setError] = useState('')
  const [cancelling, setCancelling] = useState(false)

  useEffect(() => {
    api.order(id).then(setOrder).catch(err => setLoadError(errorMessage(err)))
  }, [id])

  async function cancelOrder() {
    if (!window.confirm('Siparişi iptal etmek istediğinize emin misiniz?')) return
    setCancelling(true)
    setError('')
    try {
      setOrder(await api.cancelOrder(id))
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCancelling(false)
    }
  }

  if (loadError) {
    return (
      <div className="card empty">
        <p>{loadError}</p>
        <Link to="/orders">Siparişlerime dön</Link>
      </div>
    )
  }
  if (!order) return <p className="muted">Sipariş yükleniyor…</p>

  return (
    <div className="stack">
      <Link to="/orders" className="small">← Siparişlerim</Link>
      <div className="page-header">
        <h1>Sipariş {order.id.slice(0, 8).toUpperCase()}</h1>
        {canCancel(order.status) && (
          <button className="button danger" disabled={cancelling} onClick={cancelOrder}>
            {cancelling ? 'İptal ediliyor…' : 'Siparişi iptal et'}
          </button>
        )}
      </div>
      {justCreated && (
        <p className="alert success">
          Siparişiniz alındı. Bu demo mağazada ödeme alınmaz; sipariş yönetici tarafından onaylanınca durumu güncellenir.
        </p>
      )}
      {error && <p className="alert error">{error}</p>}
      <OrderDetails order={order} />
    </div>
  )
}
