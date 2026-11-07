import type {
  Account, Address, Cart, Category, Currency, Order, OrderStatus, OrderSummary,
  PagedResult, Price, Product, ProductSummary, TokenResponse,
} from '../types/api'
import { translateProblem } from './errors'

const baseUrl = import.meta.env.VITE_API_URL ?? ''
const refreshTokenKey = 'ecommerce.refreshToken'

// The access token lives only in memory. The refresh token is kept in sessionStorage
// so a page reload keeps the session, and it is removed when the tab is closed.
let accessToken: string | null = null
let refreshPromise: Promise<boolean> | null = null
let onSessionExpired: (() => void) | null = null

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}

export function setSessionExpiredHandler(handler: () => void) {
  onSessionExpired = handler
}

export function hasStoredSession() {
  return sessionStorage.getItem(refreshTokenKey) !== null
}

function saveTokens(tokens: TokenResponse) {
  accessToken = tokens.accessToken
  sessionStorage.setItem(refreshTokenKey, tokens.refreshToken)
}

function clearTokens() {
  accessToken = null
  sessionStorage.removeItem(refreshTokenKey)
}

export function refreshSession(): Promise<boolean> {
  const refreshToken = sessionStorage.getItem(refreshTokenKey)
  if (!refreshToken) return Promise.resolve(false)

  refreshPromise ??= fetch(`${baseUrl}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
    .then(async response => {
      if (!response.ok) {
        clearTokens()
        return false
      }
      saveTokens(await response.json())
      return true
    })
    .catch(() => false)
    .finally(() => {
      refreshPromise = null
    })
  return refreshPromise
}

interface RequestOptions {
  method?: string
  body?: unknown
  headers?: Record<string, string>
}

async function request<T>(path: string, options: RequestOptions = {}, canRefresh = true): Promise<T> {
  const headers: Record<string, string> = { ...options.headers }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`

  let response: Response
  try {
    response = await fetch(baseUrl + path, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    throw new ApiError(0, 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.')
  }

  if (response.status === 401 && canRefresh && hasStoredSession()) {
    if (await refreshSession()) return request<T>(path, options, false)
    onSessionExpired?.()
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, translateProblem(response.status, problem))
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

function query(params: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

export interface ProductFilter {
  categoryId?: string
  search?: string
  page?: number
  pageSize?: number
}

export interface ProductInput {
  categoryId: string
  name: string
  description: string | null
}

export interface VariantInput {
  sku: string
  stockQuantity: number
  optionValueIds: string[]
  prices: Price[]
}

export interface CheckoutInput {
  expectedCurrency: Currency
  expectedTotal: number
  shippingAddress: Address
}

export type OrderAction = 'confirm' | 'ship' | 'deliver' | 'cancel'

export const api = {
  async login(email: string, password: string) {
    saveTokens(await request<TokenResponse>('/api/auth/login', { method: 'POST', body: { email, password } }, false))
  },
  async logout() {
    try {
      await request<void>('/api/auth/logout', { method: 'POST' }, false)
    } finally {
      clearTokens()
    }
  },
  register: (email: string, password: string) =>
    request<Account>('/api/auth/register', { method: 'POST', body: { email, password } }),
  me: () => request<Account>('/api/auth/me'),

  categories: () => request<PagedResult<Category>>('/api/categories?pageSize=100'),
  createCategory: (name: string, description: string | null) =>
    request<Category>('/api/categories', { method: 'POST', body: { name, description } }),
  updateCategory: (id: string, name: string, description: string | null) =>
    request<Category>(`/api/categories/${id}`, { method: 'PUT', body: { name, description } }),

  products: (filter: ProductFilter) => request<PagedResult<ProductSummary>>(`/api/products${query({ ...filter })}`),
  product: (id: string) => request<Product>(`/api/products/${id}`),

  adminProducts: (filter: ProductFilter) =>
    request<PagedResult<ProductSummary>>(`/api/admin/products${query({ ...filter })}`),
  adminProduct: (id: string) => request<Product>(`/api/admin/products/${id}`),
  createProduct: (input: ProductInput) => request<Product>('/api/products', { method: 'POST', body: input }),
  updateProduct: (id: string, input: ProductInput) =>
    request<Product>(`/api/products/${id}`, { method: 'PUT', body: input }),
  setProductAvailability: (id: string, isActive: boolean) =>
    request<Product>(`/api/admin/products/${id}/availability`, { method: 'PUT', body: { isActive } }),
  addOption: (productId: string, name: string, values: string[]) =>
    request<Product>(`/api/products/${productId}/options`, { method: 'POST', body: { name, values } }),
  addOptionValue: (productId: string, optionId: string, value: string) =>
    request<Product>(`/api/products/${productId}/options/${optionId}/values`, { method: 'POST', body: { value } }),
  addVariant: (productId: string, input: VariantInput) =>
    request<Product>(`/api/products/${productId}/variants`, { method: 'POST', body: input }),
  setVariantPrice: (productId: string, variantId: string, currency: Currency, amount: number) =>
    request<Product>(`/api/products/${productId}/variants/${variantId}/price`, {
      method: 'PUT',
      body: { currency, amount },
    }),
  setVariantStock: (productId: string, variantId: string, stockQuantity: number) =>
    request<Product>(`/api/products/${productId}/variants/${variantId}/stock`, {
      method: 'PUT',
      body: { stockQuantity },
    }),

  cart: () => request<Cart>('/api/cart'),
  addToCart: (productVariantId: string, quantity: number, currency: Currency) =>
    request<Cart>('/api/cart/items', { method: 'POST', body: { productVariantId, quantity, currency } }),
  setCartQuantity: (variantId: string, quantity: number) =>
    request<Cart>(`/api/cart/items/${variantId}`, { method: 'PUT', body: { quantity } }),
  removeFromCart: (variantId: string) => request<void>(`/api/cart/items/${variantId}`, { method: 'DELETE' }),
  clearCart: () => request<void>('/api/cart', { method: 'DELETE' }),
  setCartCurrency: (currency: Currency) => request<Cart>('/api/cart/currency', { method: 'PUT', body: { currency } }),

  checkout: (idempotencyKey: string, input: CheckoutInput) =>
    request<Order>('/api/orders', { method: 'POST', body: input, headers: { 'Idempotency-Key': idempotencyKey } }),
  orders: (page: number) => request<PagedResult<OrderSummary>>(`/api/orders${query({ page })}`),
  order: (id: string) => request<Order>(`/api/orders/${id}`),
  cancelOrder: (id: string) => request<Order>(`/api/orders/${id}/cancel`, { method: 'POST' }),

  adminOrders: (status: OrderStatus | undefined, page: number) =>
    request<PagedResult<OrderSummary>>(`/api/admin/orders${query({ status, page })}`),
  adminOrder: (id: string) => request<Order>(`/api/admin/orders/${id}`),
  changeOrderStatus: (id: string, action: OrderAction) =>
    request<Order>(`/api/admin/orders/${id}/${action}`, { method: 'POST' }),
}
