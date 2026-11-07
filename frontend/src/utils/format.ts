import type { Currency, OrderStatus } from '../types/api'

export function formatPrice(amount: number, currency: Currency) {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency }).format(amount)
}

export function formatDate(value: string) {
  return new Date(value).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}

export const orderStatusLabels: Record<OrderStatus, string> = {
  Pending: 'Beklemede',
  Confirmed: 'Onaylandı',
  Shipped: 'Kargoda',
  Delivered: 'Teslim edildi',
  Cancelled: 'İptal edildi',
}

export const cartIssueLabels: Record<string, string> = {
  insufficient_stock: 'Stok yetersiz',
  price_unavailable: 'Bu para biriminde fiyatı yok',
  product_inactive: 'Satıştan kaldırıldı',
  variant_unavailable: 'Ürün bulunamadı',
}

export function canCancel(status: OrderStatus) {
  return status === 'Pending' || status === 'Confirmed'
}
