import { NavLink, Outlet } from 'react-router-dom'

export default function AdminLayout() {
  return (
    <div>
      <nav className="admin-nav" aria-label="Yönetim menüsü">
        <NavLink to="/admin/products">Ürünler</NavLink>
        <NavLink to="/admin/categories">Kategoriler</NavLink>
        <NavLink to="/admin/orders">Siparişler</NavLink>
      </nav>
      <Outlet />
    </div>
  )
}
