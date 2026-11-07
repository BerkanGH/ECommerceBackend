// Creates the demo catalog in scripts/demo-data.json through the public API.
// Run it only against a development database:
//   $env:ADMIN_EMAIL = "..."; $env:ADMIN_PASSWORD = "..."; node scripts/seed-demo.mjs
// A category that already exists is skipped together with its products,
// so the script never changes existing catalog data.
import { readFile } from 'node:fs/promises'

const apiUrl = process.env.API_URL ?? 'http://localhost:5106'
const { ADMIN_EMAIL: email, ADMIN_PASSWORD: password } = process.env
if (!email || !password) {
  console.error('ADMIN_EMAIL ve ADMIN_PASSWORD ortam değişkenlerini ayarlayın.')
  process.exit(1)
}

const data = JSON.parse(await readFile(new URL('./demo-data.json', import.meta.url), 'utf8'))
let token = ''

async function call(method, path, body) {
  const response = await fetch(apiUrl + path, {
    method,
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) throw new Error(`${method} ${path} -> ${response.status} ${await response.text()}`)
  return response.status === 204 ? null : response.json()
}

try {
  token = (await call('POST', '/api/auth/login', { email, password })).accessToken
  const existing = new Set((await call('GET', '/api/categories?pageSize=100')).items.map(category => category.name))

  for (const categoryData of data.categories) {
    if (existing.has(categoryData.name)) {
      console.log(`Atlandı: "${categoryData.name}" kategorisi zaten var.`)
      continue
    }
    const category = await call('POST', '/api/categories', categoryData)
    console.log(`Kategori oluşturuldu: ${category.name}`)

    for (const item of data.products.filter(product => product.category === categoryData.name)) {
      let product = await call('POST', '/api/products', {
        categoryId: category.id,
        name: item.name,
        description: item.description,
        imageUrl: item.imageUrl,
      })
      for (const option of item.options) {
        product = await call('POST', `/api/products/${product.id}/options`, option)
      }
      const valueIds = new Map(product.options.flatMap(option => option.values.map(value => [value.value, value.id])))
      for (const variant of item.variants) {
        await call('POST', `/api/products/${product.id}/variants`, {
          sku: variant.sku,
          stockQuantity: variant.stock,
          optionValueIds: variant.values.map(value => valueIds.get(value)),
          prices: Object.entries(variant.prices).map(([currency, amount]) => ({ currency, amount })),
        })
      }
      console.log(`  Ürün oluşturuldu: ${item.name} (${item.variants.length} varyant)`)
    }
  }
  console.log('Demo verisi hazır.')
} catch (error) {
  console.error(`Hata: ${error.message}`)
  process.exitCode = 1
}
