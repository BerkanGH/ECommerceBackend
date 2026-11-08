# ECommerce Backend

[![Build and test](https://github.com/BerkanGH/ECommerceBackend/actions/workflows/ci.yml/badge.svg)](https://github.com/BerkanGH/ECommerceBackend/actions/workflows/ci.yml)

Tek mağazalı bir e-ticaret sitesi için ASP.NET Core Web API. Ürün kataloğu, müşteri
sepeti ve sipariş akışını içerir. Gerçek ödeme veya kargo entegrasyonu yoktur;
odak noktası iş kuralları, yetkilendirme ve stok tutarlılığıdır.
`frontend/` klasöründe bu API'yi kullanan React arayüzü bulunur.

## Özellikler

- Kategori ve ürün yönetimi; ürüne özel seçenekler (renk, beden vb.) ve varyantlar.
- Ürün başına isteğe bağlı bir görsel adresi (sadece `https://`). Dosya yükleme servisi yoktur.
- Her varyant için ayrı TRY ve USD fiyatı ile stok miktarı. Kur dönüşümü yapılmaz.
- ASP.NET Core Identity ile kayıt, giriş, refresh token, parola değiştirme ve çıkış.
- Customer ve Admin rolleri. Kayıt olan herkes Customer olur.
- Müşteriye ait sepet. Fiyat her zaman sunucuda katalogdan hesaplanır.
- Sipariş oluşturma tek transaction içinde yapılır: stok düşer, sepet temizlenir.
  `Idempotency-Key` başlığı aynı isteğin iki kez sipariş oluşturmasını engeller.
- Sipariş iptalinde stok bir kez iade edilir. Admin siparişi onaylar, kargolar, teslim eder.
- Hatalar ProblemDetails formatında döner. Giriş uçlarında rate limit vardır.

## Teknolojiler

.NET 10, ASP.NET Core Web API, EF Core 10, PostgreSQL, ASP.NET Core Identity, MediatR, xUnit.
Frontend: React, TypeScript, Vite ve React Router.

| Proje | Görevi |
| --- | --- |
| `ECommerce.Domain` | Entity'ler ve iş kuralları (ürün, sepet, sipariş) |
| `ECommerce.Application` | MediatR command/query handler'ları, DTO'lar, repository arayüzleri |
| `ECommerce.Infrastructure` | EF Core DbContext, mapping'ler, migration'lar, repository'ler, Identity |
| `ECommerce.Api` | Controller'lar, kimlik doğrulama, hata yönetimi |
| `tests/` | Unit testler ve PostgreSQL üzerinde çalışan integration testler |
| `frontend/` | Müşteri ve yönetim arayüzü |

Tasarım kararlarının açıklaması: [docs/Architecture.md](docs/Architecture.md).

## Çalıştırma

Gerekenler: .NET 10 SDK ve PostgreSQL. Önce `ecommerce` adında boş bir veritabanı oluşturun.
Uygulama migration'ları kendisi uygulamaz; aşağıdaki `database update` komutu gerekir.

```powershell
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:ECommerce" "Host=localhost;Port=5432;Database=ecommerce;Username=KULLANICI;Password=PAROLA" --project ECommerce.Api
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project ECommerce.Infrastructure --startup-project ECommerce.Api
dotnet run --project ECommerce.Api --launch-profile http
```

- API: http://localhost:5106
- OpenAPI: http://localhost:5106/openapi/v1.json (sadece Development)
- Health: `/health/live`, `/health/ready` (veritabanı bağlantısı ve bekleyen migration kontrolü)

Bağlantı dizesi yoksa uygulama açılışta hata verir.

### Docker ile PostgreSQL (isteğe bağlı)

```powershell
Copy-Item .env.example .env   # .env içindeki parolayı değiştirin
docker compose up -d postgres
```

Compose PostgreSQL'i **5433** portunda açar; bağlantı dizesinde `Port=5433` kullanın.

### İlk admin hesabı

Kayıt endpoint'i sadece Customer oluşturur. Admin hesabı ayrı bir komutla oluşturulur
(HTTP sunucusu başlatmadan çalışır ve çıkar):

```powershell
dotnet user-secrets set "AdminBootstrap:Email" "admin@example.com" --project ECommerce.Api
dotnet user-secrets set "AdminBootstrap:Password" "GucluBirParola1!" --project ECommerce.Api
dotnet run --project ECommerce.Api -- --bootstrap-admin
```

Örnek istekler `ECommerce.Api` altındaki `Identity.http`, `Catalog.http`, `Cart.http`
ve `Orders.http` dosyalarındadır.

## Frontend

Gerekenler: Node.js 20.19+ veya 22.12+. API çalışırken:

```powershell
cd frontend
npm install
npm run dev
```

Arayüz http://localhost:5173 adresinde açılır. Geliştirme sunucusu `/api` isteklerini
http://localhost:5106 adresine yönlendirir, bu yüzden backend'de CORS ayarı gerekmez.
Farklı bir adres için `frontend/.env.example` dosyasını `.env` olarak kopyalayıp düzenleyin.
`npm run build` production derlemesini `frontend/dist` klasörüne üretir.

Müşteri tarafında ürün listesi, kategori filtresi ve arama, varyant seçimi, sepet,
sipariş verme, sipariş geçmişi, iptal ve şifre değiştirme vardır. Admin hesabıyla giriş
yapınca kategori, ürün, görsel adresi, seçenek, varyant, fiyat, stok ve sipariş durumu
yönetimi açılır.
Sayfaları rol bazında gizlemek sadece arayüz kolaylığıdır; yetki kontrolü API'dedir.

Access token sadece bellekte, refresh token ise sekme kapanınca silinen `sessionStorage`'da
tutulur; şifre tarayıcıda saklanmaz. Görseli olmayan veya yüklenemeyen ürünlerde sade bir
varsayılan görsel gösterilir. Production'da arayüz API ile aynı adresten sunulmalı veya
API'ye CORS ayarı eklenmelidir.

### Demo verisi (isteğe bağlı)

`scripts/demo-data.json` 5 kategori ve 12 örnek ürün içerir. Görseller Unsplash'ten,
[Unsplash Lisansı](https://unsplash.com/license) ile kullanılır. Betik veriyi admin hesabıyla
API üzerinden oluşturur; aynı isimde bir kategori varsa onu kullanır, aynı isimde bir
ürün varsa atlar. Sadece kayıt ekler, mevcut kayıtları değiştirmez. Sadece geliştirme veritabanında, API çalışırken kullanın:

```powershell
$env:ADMIN_EMAIL = "admin@example.com"
$env:ADMIN_PASSWORD = "ADMIN_PAROLASI"
node scripts/seed-demo.mjs
```

## Endpoint'ler

| Alan | Endpoint'ler | Erişim |
| --- | --- | --- |
| Hesap | `POST /api/auth/register`, `login`, `refresh`; `GET /api/auth/me`; `POST /api/auth/change-password`, `logout` | Herkese açık / giriş yapmış kullanıcı |
| Katalog okuma | `GET /api/categories`, `GET /api/products`, `GET /api/products/{id}` | Herkese açık |
| Katalog yazma | `POST/PUT /api/categories`, `POST/PUT /api/products`, seçenek, varyant, fiyat, stok | Admin |
| Admin ürün | `GET /api/admin/products`, `PUT /api/admin/products/{id}/availability` | Admin |
| Sepet | `GET/DELETE /api/cart`, `POST /api/cart/items`, `PUT/DELETE /api/cart/items/{variantId}`, `PUT /api/cart/currency` | Customer |
| Sipariş | `POST /api/orders` (`Idempotency-Key` başlığı ile), `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel` | Customer (sadece kendi siparişleri) |
| Admin sipariş | `GET /api/admin/orders`, `GET /api/admin/orders/{id}`, `POST /api/admin/orders/{id}/confirm`, `ship`, `deliver`, `cancel` | Admin |

## Testler

Unit testler veritabanı gerektirmez:

```powershell
dotnet test tests/ECommerce.UnitTests
```

Integration testler gerçek PostgreSQL kullanır. Test veritabanını önceden oluşturun;
testler bu veritabanında rastgele isimli bir şema açar, migration'ları uygular ve
bitince sadece o şemayı siler.

```powershell
$env:ECOMMERCE_TEST_CONNECTION = "Host=localhost;Port=5432;Database=ecommerce_tests;Username=KULLANICI;Password=PAROLA"
dotnet test ECommerce.slnx
```

Değişken ayarlı değilse integration testler "skipped" olarak raporlanır.
GitHub Actions her push'ta build, migration kontrolü, tüm testler, frontend derlemesi ve Docker imaj kontrolünü çalıştırır.

## Sınırlar

- Sipariş oluşturmak ödeme almak anlamına gelmez; sipariş durumları admin tarafından elle ilerletilir.
- E-posta doğrulama, parola sıfırlama, MFA, iade ve kargo ücreti yoktur.
- Çıkış yapınca refresh token'lar geçersiz olur, ancak verilmiş access token 15 dakikalık süresi dolana kadar geçerli kalır.
- MediatR 13 ve sonrası ticari lisanslıdır; lisans anahtarı olmadan çalışır ama uyarı loglar.
