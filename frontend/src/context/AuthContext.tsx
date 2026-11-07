import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, hasStoredSession, refreshSession, setSessionExpiredHandler } from '../services/api'
import type { Account } from '../types/api'

interface AuthContextValue {
  user: Account | null
  loading: boolean
  isAdmin: boolean
  isCustomer: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<Account | null>(null)
  const [loading, setLoading] = useState(hasStoredSession)

  useEffect(() => {
    setSessionExpiredHandler(() => setUser(null))
    if (!hasStoredSession()) return

    refreshSession()
      .then(ok => (ok ? api.me() : null))
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false))
  }, [])

  async function login(email: string, password: string) {
    await api.login(email, password)
    setUser(await api.me())
  }

  async function register(email: string, password: string) {
    await api.register(email, password)
    await login(email, password)
  }

  async function logout() {
    try {
      await api.logout()
    } finally {
      setUser(null)
    }
  }

  const value: AuthContextValue = {
    user,
    loading,
    isAdmin: user?.roles.includes('Admin') ?? false,
    isCustomer: user?.roles.includes('Customer') ?? false,
    login,
    register,
    logout,
  }
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
