import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, errorMessage } from '../../services/api'
import type { Category, Currency, Price, Product, Variant } from '../../types/api'

function priceOf(variant: Variant, currency: Currency) {
  return variant.prices.find(price => price.currency === currency)?.amount
}

function VariantRow({ product, variant, valueNames, onSaved }: {
  product: Product
  variant: Variant
  valueNames: Map<string, string>
  onSaved: (product: Product) => void
}) {
  const [stock, setStock] = useState(String(variant.stockQuantity))
  const [priceTry, setPriceTry] = useState(String(priceOf(variant, 'TRY') ?? ''))
  const [priceUsd, setPriceUsd] = useState(String(priceOf(variant, 'USD') ?? ''))
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  async function save() {
    setSaving(true)
    setError('')
    try {
      let updated = product
      if (Number(stock) !== variant.stockQuantity) {
        updated = await api.setVariantStock(product.id, variant.id, Number(stock))
      }
      for (const [currency, value] of [['TRY', priceTry], ['USD', priceUsd]] as [Currency, string][]) {
        if (value !== '' && Number(value) !== priceOf(variant, currency)) {
          updated = await api.setVariantPrice(product.id, variant.id, currency, Number(value))
        }
      }
      onSaved(updated)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <tr>
      <td>
        <strong>{variant.sku}</strong>
        <div className="muted small">{variant.optionValueIds.map(id => valueNames.get(id)).join(', ') || 'Varsayılan'}</div>
        {error && <div className="text-danger small">{error}</div>}
      </td>
      <td>
        <input type="number" min={0} step={1} className="quantity-input" aria-label="Stok"
          value={stock} onChange={e => setStock(e.target.value)} />
      </td>
      <td>
        <input type="number" min={0} step={0.01} className="quantity-input" aria-label="TRY fiyatı"
          value={priceTry} onChange={e => setPriceTry(e.target.value)} />
      </td>
      <td>
        <input type="number" min={0} step={0.01} className="quantity-input" aria-label="USD fiyatı"
          placeholder="—" value={priceUsd} onChange={e => setPriceUsd(e.target.value)} />
      </td>
      <td className="align-right">
        <button className="button secondary small" disabled={saving} onClick={save}>Kaydet</button>
      </td>
    </tr>
  )
}

export default function AdminProductEdit() {
  const { id } = useParams()
  const isNew = id === undefined
  const navigate = useNavigate()

  const [categories, setCategories] = useState<Category[]>([])
  const [product, setProduct] = useState<Product | null>(null)
  const [details, setDetails] = useState({ categoryId: '', name: '', description: '' })
  const [optionName, setOptionName] = useState('')
  const [optionValues, setOptionValues] = useState('')
  const [newValues, setNewValues] = useState<Record<string, string>>({})
  const [variantForm, setVariantForm] = useState({ sku: '', stock: '0', priceTry: '', priceUsd: '' })
  const [variantSelection, setVariantSelection] = useState<Record<string, string>>({})
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api.categories().then(data => setCategories(data.items)).catch(err => setError(errorMessage(err)))
  }, [])

  useEffect(() => {
    if (!id) return
    api.adminProduct(id)
      .then(data => {
        setProduct(data)
        setDetails({ categoryId: data.categoryId, name: data.name, description: data.description ?? '' })
      })
      .catch(err => setError(errorMessage(err)))
  }, [id])

  async function run(action: () => Promise<Product>, successMessage: string) {
    setBusy(true)
    setError('')
    setMessage('')
    try {
      const updated = await action()
      setProduct(updated)
      setMessage(successMessage)
      return updated
    } catch (err) {
      setError(errorMessage(err))
      return null
    } finally {
      setBusy(false)
    }
  }

  async function saveDetails(event: FormEvent) {
    event.preventDefault()
    const input = { categoryId: details.categoryId, name: details.name.trim(), description: details.description.trim() || null }
    if (id === undefined) {
      const created = await run(() => api.createProduct(input), 'Ürün oluşturuldu. Şimdi seçenek ve varyant ekleyebilirsiniz.')
      if (created) navigate(`/admin/products/${created.id}`, { replace: true })
    } else {
      await run(() => api.updateProduct(id, input), 'Ürün bilgileri kaydedildi.')
    }
  }

  async function addOption(event: FormEvent) {
    event.preventDefault()
    if (!product) return
    const values = optionValues.split(',').map(value => value.trim()).filter(Boolean)
    if (await run(() => api.addOption(product.id, optionName.trim(), values), 'Seçenek eklendi.')) {
      setOptionName('')
      setOptionValues('')
    }
  }

  async function addValue(optionId: string) {
    if (!product || !newValues[optionId]?.trim()) return
    if (await run(() => api.addOptionValue(product.id, optionId, newValues[optionId].trim()), 'Değer eklendi.')) {
      setNewValues(current => ({ ...current, [optionId]: '' }))
    }
  }

  async function addVariant(event: FormEvent) {
    event.preventDefault()
    if (!product) return
    const prices: Price[] = [{ currency: 'TRY', amount: Number(variantForm.priceTry) }]
    if (variantForm.priceUsd !== '') prices.push({ currency: 'USD', amount: Number(variantForm.priceUsd) })
    const input = {
      sku: variantForm.sku.trim(),
      stockQuantity: Number(variantForm.stock),
      optionValueIds: product.options.map(option => variantSelection[option.id]).filter(Boolean),
      prices,
    }
    if (await run(() => api.addVariant(product.id, input), 'Varyant eklendi.')) {
      setVariantForm({ sku: '', stock: '0', priceTry: '', priceUsd: '' })
      setVariantSelection({})
    }
  }

  if (!isNew && !product) {
    return error ? <p className="alert error">{error}</p> : <p className="muted">Ürün yükleniyor…</p>
  }

  const valueNames = new Map(product?.options.flatMap(option =>
    option.values.map(value => [value.id, `${option.name}: ${value.value}`] as [string, string])) ?? [])
  const optionsLocked = (product?.variants.length ?? 0) > 0

  return (
    <div className="stack">
      <Link to="/admin/products" className="small">← Ürünler</Link>
      <div className="page-header">
        <h1>{isNew ? 'Yeni ürün' : product?.name}</h1>
        {product && (
          <div className="row">
            <span className={product.isActive ? 'status Delivered' : 'status Cancelled'}>
              {product.isActive ? 'Satışta' : 'Satışa kapalı'}
            </span>
            <button className="button secondary small" disabled={busy}
              onClick={() => run(() => api.setProductAvailability(product.id, !product.isActive),
                product.isActive ? 'Ürün satışa kapatıldı.' : 'Ürün satışa açıldı.')}>
              {product.isActive ? 'Satışa kapat' : 'Satışa aç'}
            </button>
          </div>
        )}
      </div>
      {error && <p className="alert error">{error}</p>}
      {message && <p className="alert success">{message}</p>}

      <form className="card form" onSubmit={saveDetails}>
        <h2>Ürün bilgileri</h2>
        <div className="form-grid">
          <label>
            Ad
            <input required maxLength={100} value={details.name} onChange={e => setDetails({ ...details, name: e.target.value })} />
          </label>
          <label>
            Kategori
            <select required value={details.categoryId} onChange={e => setDetails({ ...details, categoryId: e.target.value })}>
              <option value="">Kategori seçin</option>
              {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
            </select>
          </label>
          <label className="full">
            Açıklama
            <textarea maxLength={1000} value={details.description}
              onChange={e => setDetails({ ...details, description: e.target.value })} />
          </label>
        </div>
        {categories.length === 0 && (
          <p className="alert warning small">Önce <Link to="/admin/categories">bir kategori oluşturun</Link>.</p>
        )}
        <div>
          <button className="button" disabled={busy}>{isNew ? 'Ürünü oluştur' : 'Bilgileri kaydet'}</button>
        </div>
      </form>

      {product && (
        <>
          <div className="card stack">
            <h2>Seçenekler</h2>
            <p className="muted small">
              Renk veya beden gibi seçenekler ürüne özeldir. Seçeneksiz bir üründe tek bir varsayılan varyant olur.
              {optionsLocked && ' Varyant oluşturulduktan sonra seçenekler değiştirilemez.'}
            </p>
            {product.options.length === 0 && <p className="muted">Bu üründe seçenek yok.</p>}
            {product.options.map(option => (
              <div key={option.id} className="option-group">
                <strong>{option.name}</strong>
                <div className="row">
                  <span className="small">{option.values.map(value => value.value).join(', ')}</span>
                  {!optionsLocked && (
                    <>
                      <input aria-label={`${option.name} için yeni değer`} placeholder="Yeni değer" maxLength={100}
                        value={newValues[option.id] ?? ''}
                        onChange={e => setNewValues(current => ({ ...current, [option.id]: e.target.value }))} />
                      <button className="button secondary small" disabled={busy} onClick={() => addValue(option.id)}>Ekle</button>
                    </>
                  )}
                </div>
              </div>
            ))}
            {!optionsLocked && (
              <form className="form-grid" onSubmit={addOption}>
                <label>
                  Seçenek adı
                  <input required maxLength={100} placeholder="ör. Renk" value={optionName} onChange={e => setOptionName(e.target.value)} />
                </label>
                <label>
                  Değerler
                  <input required placeholder="ör. Siyah, Beyaz" value={optionValues} onChange={e => setOptionValues(e.target.value)} />
                  <span className="field-hint">Virgülle ayırın.</span>
                </label>
                <div>
                  <button className="button secondary" disabled={busy}>Seçenek ekle</button>
                </div>
              </form>
            )}
          </div>

          <div className="card stack">
            <h2>Varyantlar, stok ve fiyat</h2>
            {product.variants.length === 0 ? (
              <p className="muted">Henüz varyant yok. Ürün satılabilmesi için en az bir varyant gerekir.</p>
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Varyant</th>
                      <th>Stok</th>
                      <th>TRY</th>
                      <th>USD</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {product.variants.map(variant => (
                      <VariantRow key={variant.id} product={product} variant={variant} valueNames={valueNames}
                        onSaved={updated => {
                          setProduct(updated)
                          setMessage('Varyant güncellendi.')
                        }} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <form className="form" onSubmit={addVariant}>
              <h3>Yeni varyant</h3>
              <div className="form-grid">
                <label>
                  SKU
                  <input required maxLength={100} value={variantForm.sku} onChange={e => setVariantForm({ ...variantForm, sku: e.target.value })} />
                </label>
                <label>
                  Stok
                  <input required type="number" min={0} step={1} value={variantForm.stock}
                    onChange={e => setVariantForm({ ...variantForm, stock: e.target.value })} />
                </label>
                {product.options.map(option => (
                  <label key={option.id}>
                    {option.name}
                    <select required value={variantSelection[option.id] ?? ''}
                      onChange={e => setVariantSelection(current => ({ ...current, [option.id]: e.target.value }))}>
                      <option value="">Seçin</option>
                      {option.values.map(value => <option key={value.id} value={value.id}>{value.value}</option>)}
                    </select>
                  </label>
                ))}
                <label>
                  TRY fiyatı
                  <input required type="number" min={0} step={0.01} value={variantForm.priceTry}
                    onChange={e => setVariantForm({ ...variantForm, priceTry: e.target.value })} />
                </label>
                <label>
                  USD fiyatı
                  <input type="number" min={0} step={0.01} value={variantForm.priceUsd}
                    onChange={e => setVariantForm({ ...variantForm, priceUsd: e.target.value })} />
                  <span className="field-hint">İsteğe bağlı.</span>
                </label>
              </div>
              <div>
                <button className="button" disabled={busy}>Varyant ekle</button>
              </div>
            </form>
          </div>
        </>
      )}
    </div>
  )
}
