import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api, errorMessage } from '../../services/api'
import type { Category } from '../../types/api'

export default function AdminCategories() {
  const [categories, setCategories] = useState<Category[] | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [editing, setEditing] = useState<Category | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setCategories((await api.categories()).items)
  }, [])

  useEffect(() => {
    load().catch(err => setError(errorMessage(err)))
  }, [load])

  async function save(action: () => Promise<unknown>) {
    setBusy(true)
    setError('')
    try {
      await action()
      await load()
      return true
    } catch (err) {
      setError(errorMessage(err))
      return false
    } finally {
      setBusy(false)
    }
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    if (await save(() => api.createCategory(name.trim(), description.trim() || null))) {
      setName('')
      setDescription('')
    }
  }

  async function handleUpdate(event: FormEvent) {
    event.preventDefault()
    if (!editing) return
    if (await save(() => api.updateCategory(editing.id, editing.name.trim(), editing.description?.trim() || null))) {
      setEditing(null)
    }
  }

  return (
    <div className="stack">
      <h1>Kategoriler</h1>
      {error && <p className="alert error">{error}</p>}

      <form className="card form" onSubmit={handleCreate}>
        <h2>Yeni kategori</h2>
        <div className="form-grid">
          <label>
            Ad
            <input required maxLength={100} value={name} onChange={e => setName(e.target.value)} />
          </label>
          <label>
            Açıklama
            <input maxLength={1000} value={description} onChange={e => setDescription(e.target.value)} />
          </label>
        </div>
        <div>
          <button className="button" disabled={busy}>Kategori ekle</button>
        </div>
      </form>

      <div className="card table-wrap">
        {!categories && <p className="muted">Yükleniyor…</p>}
        {categories?.length === 0 && <p className="empty">Henüz kategori yok.</p>}
        {categories && categories.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Ad</th>
                <th>Açıklama</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {categories.map(category =>
                editing?.id === category.id ? (
                  <tr key={category.id}>
                    <td>
                      <input required maxLength={100} form="edit-category" aria-label="Ad" value={editing.name}
                        onChange={e => setEditing({ ...editing, name: e.target.value })} />
                    </td>
                    <td>
                      <input maxLength={1000} form="edit-category" aria-label="Açıklama" value={editing.description ?? ''}
                        onChange={e => setEditing({ ...editing, description: e.target.value })} />
                    </td>
                    <td className="align-right">
                      <form id="edit-category" className="row" onSubmit={handleUpdate}>
                        <button className="button small" disabled={busy}>Kaydet</button>
                        <button type="button" className="button secondary small" onClick={() => setEditing(null)}>Vazgeç</button>
                      </form>
                    </td>
                  </tr>
                ) : (
                  <tr key={category.id}>
                    <td>{category.name}</td>
                    <td className="muted">{category.description}</td>
                    <td className="align-right">
                      <button className="button secondary small" onClick={() => setEditing(category)}>Düzenle</button>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        )}
      </div>
      <p className="muted small">Kategori silme API'de bulunmadığı için bu panelde de yoktur.</p>
    </div>
  )
}
