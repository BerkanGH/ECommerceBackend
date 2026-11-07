import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from '../services/api'
import type { Cart, Currency } from '../types/api'
import { useAuth } from './AuthContext'

interface CartContextValue {
  cart: Cart | null
  currency: Currency
  itemCount: number
  setCart: (cart: Cart) => void
  reloadCart: () => Promise<void>
  changeCurrency: (currency: Currency) => Promise<void>
}

const CartContext = createContext<CartContextValue | null>(null)

export function CartProvider({ children }: { children: ReactNode }) {
  const { isCustomer } = useAuth()
  const [cart, setCart] = useState<Cart | null>(null)
  const [guestCurrency, setGuestCurrency] = useState<Currency>('TRY')

  const reloadCart = useCallback(async () => {
    setCart(isCustomer ? await api.cart() : null)
  }, [isCustomer])

  useEffect(() => {
    reloadCart().catch(() => setCart(null))
  }, [reloadCart])

  async function changeCurrency(currency: Currency) {
    if (isCustomer) setCart(await api.setCartCurrency(currency))
    else setGuestCurrency(currency)
  }

  const value: CartContextValue = {
    cart,
    currency: cart?.currency ?? guestCurrency,
    itemCount: cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0,
    setCart,
    reloadCart,
    changeCurrency,
  }
  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart() {
  const context = useContext(CartContext)
  if (!context) throw new Error('useCart must be used inside CartProvider')
  return context
}
