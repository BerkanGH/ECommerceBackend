// Creates the demo catalog in scripts/demo-data.json through the public API.
// Run it only against a development database:
//   $env:ADMIN_EMAIL = "..."; $env:ADMIN_PASSWORD = "..."; node scripts/seed-demo.mjs
// Existing categories are reused and products that already exist by name are skipped,
// so the script only adds records and never changes existing ones.
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
  const categories = new Map((await call('GET', '/api/categories?pageSize=100')).items.map(category => [category.name, category]))

  for (const categoryData of data.categories) {
    let category = categories.get(categoryData.name)
    if (category) {
      console.log(`Mevcut kategori kullanılıyor: ${category.name}`)
    } else {
      category = await call('POST', '/api/categories', categoryData)
      console.log(`Kategori oluşturuldu: ${category.name}`)
    }
    const existingProducts = new Set(
      (await call('GET', `/api/admin/products?categoryId=${category.id}&pageSize=100`)).items.map(product => product.name))

    for (const item of data.products.filter(product => product.category === categoryData.name)) {
      if (existingProducts.has(item.name)) {
        console.log(`  Atlandı: "${item.name}" zaten var.`)
        continue
      }
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
