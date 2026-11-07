// The API returns English ProblemDetails messages without error codes,
// so known messages are translated here and everything else falls back to the HTTP status.
const knownMessages: Record<string, string> = {
  'Insufficient stock.': 'Yeterli stok yok.',
  'Requested quantity exceeds available stock.': 'İstenen adet mevcut stoktan fazla.',
  'Product is not available for purchase.': 'Bu ürün şu anda satışta değil.',
  'Variant has no price in the cart currency.': 'Bu ürünün sepetinizdeki para biriminde fiyatı yok.',
  'Clear the cart before changing its currency.': 'Para birimini değiştirmek için önce sepeti boşaltın.',
  'A cart can have at most 100 different variants.': 'Sepete en fazla 100 farklı ürün eklenebilir.',
  'Your cart is empty.': 'Sepetiniz boş.',
  'Cart currency changed. Reload the cart.': 'Sepetin para birimi değişti. Sepeti kontrol edin.',
  'Prices changed. Reload the cart and confirm the current total.':
    'Fiyatlar değişti. Lütfen güncel tutarı kontrol edip tekrar onaylayın.',
  'A cart variant is no longer available.': 'Sepetinizdeki bir ürün artık satışta değil.',
  'Idempotency key was already used with a different request.':
    'Bu sipariş isteği farklı bilgilerle daha önce gönderildi. Sayfayı yenileyip tekrar deneyin.',
  'Only pending or confirmed orders can be cancelled.': 'Sadece beklemede veya onaylanmış siparişler iptal edilebilir.',
  'This order status transition is not allowed.': 'Sipariş bu duruma geçirilemez.',
  'An account with this email already exists.': 'Bu e-posta adresiyle kayıtlı bir hesap zaten var.',
  'SKU already exists.': 'Bu SKU başka bir varyantta kullanılıyor.',
  'SKU already exists in this product.': 'Bu SKU bu üründe zaten var.',
  'Variant combination already exists.': 'Bu seçenek kombinasyonuyla bir varyant zaten var.',
  'Options cannot change after variants have been created.':
    'Varyant oluşturulduktan sonra seçenekler değiştirilemez.',
  'Option name already exists.': 'Bu isimde bir seçenek zaten var.',
  'Option value already exists.': 'Bu değer zaten var.',
  'A product can have at most 10 options.': 'Bir ürüne en fazla 10 seçenek eklenebilir.',
}

const knownPrefixes: [string, string][] = [
  ['Select exactly one value from each product option', 'Her seçenekten bir değer seçin.'],
  ['Only TRY and USD are supported', 'Sadece TRY ve USD desteklenir.'],
  ['Price must fit numeric', 'Fiyat 0 veya daha büyük olmalı ve en fazla iki ondalık basamak içermeli.'],
]

const identityErrors: Record<string, string> = {
  PasswordTooShort: 'Şifre en az 10 karakter olmalı.',
  PasswordRequiresDigit: 'Şifre en az bir rakam içermeli.',
  PasswordRequiresLower: 'Şifre en az bir küçük harf içermeli.',
  PasswordRequiresUpper: 'Şifre en az bir büyük harf içermeli.',
  PasswordRequiresNonAlphanumeric: 'Şifre en az bir özel karakter içermeli (ör. ! veya *).',
  InvalidEmail: 'Geçerli bir e-posta adresi girin.',
}

const statusMessages: Record<number, string> = {
  400: 'Girilen bilgileri kontrol edin.',
  401: 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.',
  403: 'Bu işlem için yetkiniz yok.',
  404: 'Aradığınız kayıt bulunamadı.',
  409: 'İşlem şu anki verilerle çakışıyor. Sayfayı yenileyip tekrar deneyin.',
  429: 'Çok fazla deneme yaptınız. Bir dakika sonra tekrar deneyin.',
  503: 'Sunucu şu anda veritabanına ulaşamıyor. Biraz sonra tekrar deneyin.',
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function translateProblem(status: number, problem: ProblemDetails | null): string {
  const detail = problem?.detail ?? ''
  if (knownMessages[detail]) return knownMessages[detail]

  const prefix = knownPrefixes.find(([english]) => detail.startsWith(english))
  if (prefix) return prefix[1]

  const identityMessages = Object.keys(problem?.errors ?? {})
    .map(code => identityErrors[code])
    .filter(Boolean)
  if (identityMessages.length > 0) return identityMessages.join(' ')

  if (problem?.title === 'Duplicate resource') return 'Bu kayıt zaten var.'
  if (problem?.title === 'Relationship conflict') return 'İlgili kayıt bulunamadı veya hâlâ kullanımda.'

  return statusMessages[status] ?? 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}
