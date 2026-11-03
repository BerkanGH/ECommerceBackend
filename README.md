# ECommerceBackend

.NET 10 ile dört katmanlı, tek satıcılı e-ticaret MVP çalışması.

## Mevcut kapsam

Katalog API'si uygulandı: kategori oluşturma/güncelleme/listeleme; ürün oluşturma,
güncelleme, detay ve sayfalı listeleme; ürüne özel seçenek/değer ekleme; varyant
oluşturma; TRY/USD fiyat ve stok güncelleme. Kullanıcı, sepet ve sipariş henüz yok.

## Mimari

- Domain: bağımsız iş kuralları, Product üzerinden seçenek ve kombinasyon yönetimi.
- Application: MediatR command/query handler'ları, DTO ve repository arayüzleri.
- Infrastructure: EF Core, Npgsql, PostgreSQL eşlemeleri ve repository'ler.
- Api: controller'lar, istek doğrulama ve ProblemDetails hata cevapları.

Amaca özel repository kullanılır; her tablo için generic CRUD repository yoktur.
IUnitOfWork, bir kullanım senaryosundaki değişikliklerin birlikte kaydedilmesini
sağlar. Okuma sorguları DTO'lara yansıtılır; entity'ler HTTP yanıtı olarak verilmez.

## Yerel kurulum

.NET 10 SDK ve çalışan PostgreSQL gerekir. Gerçek kullanıcı adı/parolayı kaynak
koduna veya appsettings dosyasına yazmayın.

PowerShell, solution dizininde:

```powershell
dotnet restore ECommerce.slnx
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:ECommerce" "Host=localhost;Port=5432;Database=ecommerce;Username=KULLANICI;Password=PAROLA" --project ECommerce.Api
dotnet ef database update --project ECommerce.Infrastructure --startup-project ECommerce.Api
dotnet run --project ECommerce.Api --launch-profile http
```

Migration komutunu yalnızca bu projeye ayrılmış veritabanına uygulayın. API
başlangıcında otomatik migration çalıştırılmaz. Ortam değişkeniyle bağlantı vermek
için ConnectionStrings__ECommerce kullanılabilir.

API: http://localhost:5106
OpenAPI JSON: http://localhost:5106/openapi/v1.json
Liveness: http://localhost:5106/health/live (veritabanı bağlantısını ölçmez).

OpenAPI JSON bir sözleşme belgesidir; Swagger UI henüz kurulmadı.

### Güvenlik sınırı

Kimlik doğrulama henüz eklenmediği için katalog yazma endpoint'leri yalnızca
Development ortamında, Catalog:EnableDevelopmentWrites=true iken ve localhost'tan
gelen isteklerde açıktır. Diğer ortamlarda 503 döner. Bu geçici sınır yönetici
yetkilendirmesinin yerine geçmez; mevcut sürüm üretime hazır değildir.
Development ayarını internete açık bir deployment'ta kullanmayın.

### MediatR lisansı

MediatR 14.2.0 kullanılır. Geliştirme/test ve üretim kullanım koşulları aynı değildir.
Üretimden önce projenize uygun lisansı değerlendirin:
https://github.com/LuckyPennySoftware/MediatR
https://luckypennysoftware.com/

Gereken lisans anahtarı MediatR:LicenseKey User Secret veya
MediatR__LicenseKey ortam değişkeniyle verilebilir. Lisans uyarısı gizlenmez.

## Endpoint'ler

| HTTP | Yol | İşlem |
|---|---|---|
| GET | /api/categories | Sayfalı kategori listesi |
| POST | /api/categories | Kategori oluştur |
| PUT | /api/categories/{id} | Kategori güncelle |
| GET | /api/products | Sayfalı ürün listesi |
| GET | /api/products/{id} | Seçenek/fiyat/varyant içeren detay |
| POST | /api/products | Ürün oluştur |
| PUT | /api/products/{id} | Ürün bilgilerini güncelle |
| POST | /api/products/{id}/options | Değerleriyle seçenek ekle |
| POST | /api/products/{id}/options/{optionId}/values | Seçeneğe değer ekle |
| POST | /api/products/{id}/variants | Varyant oluştur |
| PUT | /api/products/{id}/variants/{variantId}/price | Bir para biriminin fiyatını belirle |
| PUT | /api/products/{id}/variants/{variantId}/stock | Stok miktarını belirle |

Listelerde page=1 ve pageSize=20 varsayılandır; pageSize en fazla 100 olabilir.
Ürün listesi categoryId ve search parametrelerini destekler. Arama terimindeki
% ve _ karakterleri joker değil, normal karakter olarak değerlendirilir.

## Örnek ürün akışı

1. Kategori oluşturun; dönen id ile ürün oluşturun.
2. Ürüne Renk seçeneğini Siyah/Beyaz değerleriyle ekleyin.
3. Beden seçeneğini M/L değerleriyle ekleyin.
4. Yanıttaki seçenek değeri kimlikleriyle Siyah/M varyantını oluşturun.
5. Aynı varyant için TRY ve USD fiyatlarını ayrı girin.

Hazır istek gövdeleri ECommerce.Api/Catalog.http dosyasında bulunur.
Varyant oluşturduktan sonra seçenek yapısı değiştirilemez. Seçeneksiz ürüne
optionValueIds=[] gönderilir ve yalnızca bir varsayılan varyant oluşturulabilir.
API'den varyant oluştururken en az bir fiyat gereklidir; diğer para birimi daha
sonra eklenebilir.

## Veri bütünlüğü

- Her seçenekten tam bir değer seçilir; başka ürüne ait değer kabul edilmez.
- SKU mağaza genelinde, kombinasyon ürün içinde benzersizdir.
- Seçenek adı ve değerler kendi kapsamlarında benzersizdir.
- Aynı varyant/para birimi için tek fiyat tutulur.
- Fiyat numeric(18,2) sınırlarında ve negatif olmayan bir tutardır.
- Stok negatif olamaz.
- Product ve Category için gölge Revision alanı eşzamanlı güncellemeyi denetler.
  Alt entity güncellemelerinde de ürünün Revision alanı değiştirilir.
- SaveChanges içindeki değişiklikler EF Core transaction'ıyla birlikte kaydedilir.
  Çakışma halinde 409 döner; istemci güncel ürünü tekrar okumalıdır.
- Ürün ağacı split query ve repeatable-read transaction ile tutarlı okunur;
  çoklu koleksiyon JOIN'lerinden doğan satır çoğalması önlenir.

Veritabanı kısıtları uygulama kontrollerini tamamlar; yalnızca önce SELECT ile
kontrol etmek eşzamanlı isteklerde benzersizlik garantisi vermez.

## Doğrulama ve sonraki aşama

Katalog akışı geçici PostgreSQL 18 örneğinde HTTP istekleriyle doğrulandı.
Kalıcı otomatik test projesi henüz yok; geçici kontroller CI testi değildir.

Sonraki aşama: Identity, müşteri/yönetici yetkileri ve geçici Development yazma
korumasının gerçek yetkilendirmeyle değiştirilmesi. Ardından sepet, sipariş,
idempotency ve stok azaltma/iptal akışları uygulanacak.

Ayrıntılı kapsam: docs/MVP-Tasarim.md.
