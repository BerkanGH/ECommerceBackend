import type { Order } from '../types/api'
import { formatDate, formatPrice } from '../utils/format'
import StatusBadge from './StatusBadge'

export default function OrderDetails({ order }: { order: Order }) {
  const address = order.shippingAddress

  return (
    <div className="split">
      <div className="card">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Ürün</th>
                <th>Adet</th>
                <th className="align-right">Birim fiyat</th>
                <th className="align-right">Tutar</th>
              </tr>
            </thead>
            <tbody>
              {order.items.map(item => (
                <tr key={item.productVariantId}>
                  <td>
                    <strong>{item.productName}</strong>
                    <div className="muted small">
                      {item.optionsSummary && <>{item.optionsSummary} · </>}SKU: {item.sku}
                    </div>
                  </td>
                  <td>{item.quantity}</td>
                  <td className="align-right">{formatPrice(item.unitPrice, order.currency)}</td>
                  <td className="align-right">{formatPrice(item.lineTotal, order.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card stack">
        <div className="summary-row">
          <span className="muted">Durum</span>
          <StatusBadge status={order.status} />
        </div>
        <div className="summary-row">
          <span className="muted">Sipariş tarihi</span>
          <span>{formatDate(order.createdAtUtc)}</span>
        </div>
        <div className="summary-row">
          <span className="muted">Son güncelleme</span>
          <span>{formatDate(order.updatedAtUtc)}</span>
        </div>
        <div className="summary-row total">
          <span>Toplam</span>
          <span>{formatPrice(order.totalAmount, order.currency)}</span>
        </div>
        <div>
          <h3>Teslimat adresi</h3>
          <p className="small">
            {address.recipientName}<br />
            {address.addressLine}<br />
            {address.postalCode} {address.city} / {address.countryCode}<br />
            {address.phone}
          </p>
        </div>
      </div>
    </div>
  )
}
