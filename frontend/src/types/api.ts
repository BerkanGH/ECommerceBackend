export type Currency = 'TRY' | 'USD'
export type OrderStatus = 'Pending' | 'Confirmed' | 'Shipped' | 'Delivered' | 'Cancelled'

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface Category {
  id: string
  name: string
  description: string | null
}

export interface Price {
  currency: Currency
  amount: number
}

export interface ProductSummary {
  id: string
  categoryId: string
  name: string
  description: string | null
  isActive: boolean
  minPriceTry: number | null
  minPriceUsd: number | null
  inStock: boolean
}

export interface OptionValue {
  id: string
  value: string
}

export interface ProductOption {
  id: string
  name: string
  values: OptionValue[]
}

export interface Variant {
  id: string
  sku: string
  stockQuantity: number
  optionValueIds: string[]
  prices: Price[]
}

export interface Product {
  id: string
  categoryId: string
  name: string
  description: string | null
  options: ProductOption[]
  variants: Variant[]
  isActive: boolean
}

export interface Account {
  id: string
  email: string
  roles: string[]
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
}

export interface CartItem {
  productVariantId: string
  productId: string | null
  productName: string | null
  sku: string | null
  quantity: number
  unitPrice: number | null
  lineTotal: number | null
  availableStock: number
  options: { name: string; value: string }[]
  isAvailable: boolean
  issue: string | null
}

export interface Cart {
  id: string | null
  currency: Currency
  items: CartItem[]
  totalAmount: number | null
  canCheckout: boolean
}

export interface Address {
  recipientName: string
  phone: string
  countryCode: string
  city: string
  postalCode: string
  addressLine: string
}

export interface OrderLine {
  productVariantId: string
  productName: string
  sku: string
  optionsSummary: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface OrderSummary {
  id: string
  userId: string
  currency: Currency
  totalAmount: number
  status: OrderStatus
  createdAtUtc: string
}

export interface Order extends OrderSummary {
  updatedAtUtc: string
  shippingAddress: Address
  items: OrderLine[]
}
