import { Link, Navigate, Route, Routes } from 'react-router-dom'
import Header from './components/Header'
import ProtectedRoute from './components/ProtectedRoute'
import Account from './pages/Account'
import AdminCategories from './pages/admin/AdminCategories'
import AdminLayout from './pages/admin/AdminLayout'
import AdminOrderDetail from './pages/admin/AdminOrderDetail'
import AdminOrders from './pages/admin/AdminOrders'
import AdminProductEdit from './pages/admin/AdminProductEdit'
import AdminProducts from './pages/admin/AdminProducts'
import Cart from './pages/Cart'
import Checkout from './pages/Checkout'
import Home from './pages/Home'
import Login from './pages/Login'
import OrderDetail from './pages/OrderDetail'
import Orders from './pages/Orders'
import ProductDetail from './pages/ProductDetail'
import Register from './pages/Register'

function NotFound() {
  return (
    <div className="card empty">
      <h2>Sayfa bulunamadı</h2>
      <Link to="/">Ürünlere dön</Link>
    </div>
  )
}

export default function App() {
  return (
    <>
      <Header />
      <main className="container page">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/products/:id" element={<ProductDetail />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />

          <Route path="/account" element={<ProtectedRoute><Account /></ProtectedRoute>} />
          <Route path="/cart" element={<ProtectedRoute role="Customer"><Cart /></ProtectedRoute>} />
          <Route path="/checkout" element={<ProtectedRoute role="Customer"><Checkout /></ProtectedRoute>} />
          <Route path="/orders" element={<ProtectedRoute role="Customer"><Orders /></ProtectedRoute>} />
          <Route path="/orders/:id" element={<ProtectedRoute role="Customer"><OrderDetail /></ProtectedRoute>} />

          <Route path="/admin" element={<ProtectedRoute role="Admin"><AdminLayout /></ProtectedRoute>}>
            <Route index element={<Navigate to="products" replace />} />
            <Route path="products" element={<AdminProducts />} />
            <Route path="products/new" element={<AdminProductEdit />} />
            <Route path="products/:id" element={<AdminProductEdit />} />
            <Route path="categories" element={<AdminCategories />} />
            <Route path="orders" element={<AdminOrders />} />
            <Route path="orders/:id" element={<AdminOrderDetail />} />
          </Route>

          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
      <footer className="footer">
        <div className="container footer-inner">
          <span>ECommerce · ASP.NET Core ve React ile hazırlanmış portföy projesi</span>
          <span>Siparişlerde gerçek ödeme alınmaz ve kargo gönderilmez.</span>
        </div>
      </footer>
    </>
  )
}
