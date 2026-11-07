import type { OrderStatus } from '../types/api'
import { orderStatusLabels } from '../utils/format'

export default function StatusBadge({ status }: { status: OrderStatus }) {
  return <span className={`status ${status}`}>{orderStatusLabels[status]}</span>
}
