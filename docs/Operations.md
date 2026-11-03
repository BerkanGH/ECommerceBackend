# Kurulum ve teslim

## Çalıştırma sırası

1. Güncel .NET 10 SDK ve PostgreSQL 18 hazırlayın.
2. ConnectionStrings:ECommerce bilgisini ECommerce.Api için User Secrets'a ekleyin.
3. dotnet restore ECommerce.slnx ve dotnet tool restore çalıştırın.
4. Migration'ları uygulayın.
5. docs/Identity.md içindeki bootstrap komutuyla yöneticiyi oluşturun.
6. API'yi başlatın; ayrı Customer hesabıyla kayıt olun.
7. Kataloğu Admin tokenıyla, sepet/siparişi Customer tokenıyla kullanın.

```powershell
dotnet build ECommerce.slnx --configuration Release
dotnet ef database update --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release
dotnet run --project ECommerce.Api --configuration Release --launch-profile http
```

Visual Studio veya başka terminalde API çalışıyorsa aynı çıktıya build yapılması
dosya kilidine takılabilir. Önce ilgili uygulamayı durdurun veya ayrı configuration
kullanın. Release derlemesi ve Development ortamı farklı kavramlardır:
http launch profile'ı Release derlemesinde de Development ayarlarını kullanır.

Connection string, gerçek parola, token, sertifika veya Data Protection anahtarlarını
repository'ye koymayın. .http dosyalarındaki yer tutucuları gerçek bilgilerle
değiştirirseniz bu değişiklikleri commit etmeyin. User Secrets yalnızca geliştirme
içindir; üretimde uygun secret yönetimi gerekir.

## İsteğe bağlı Docker PostgreSQL

```powershell
Copy-Item .env.example .env
```

.env içindeki REPLACE_WITH_A_LONG_RANDOM_PASSWORD değerini yerelde değiştirin.
Sonra:

```powershell
docker compose config --quiet
docker compose up -d postgres
docker compose ps
```

Varsayılan host portu 5433'tür; kurulu PostgreSQL'in 5432 portuyla çakışmaz.
Connection string'de Port=5433, Database=ecommerce, Username=ecommerce ve seçtiğiniz
parolayı kullanın. Ardından normal dotnet migration/API komutlarını çalıştırın.
Compose yalnızca veritabanını başlatır; API host'ta çalışır.
.env otomatik olarak dotnet uygulamasına aktarılmaz; User Secrets veya ortam
değişkeniyle API bağlantısını ayrıca vermelisiniz.

Veriler postgres_data volume'unda tutulur. docker compose down konteyneri kaldırır
ama veriyi silmez; down -v kullanmak veritabanını siler. Bu komutu veri kaybı
istediğinizden emin olmadıkça kullanmayın. POSTGRES_PASSWORD ilk initialization
içindir; mevcut volume parolasını .env değiştirerek otomatik değiştiremezsiniz.

## API image

```powershell
docker build -t ecommerce-api:local .
```

Dockerfile .NET 10 SDK ile publish eder, aspnet runtime ve non-root kullanıcıyla
çalıştırır. Image'a parola eklenmez. Runtime'da ConnectionStrings__ECommerce
verilmelidir. Konteyner içindeki localhost host bilgisayarını göstermez; uygun
ağ/host adresi kullanılmalıdır. Host'ta Docker Desktop için host.docker.internal
adresini kendi bağlantı ortamınıza göre değerlendirin.

Image Production ortamında çalışır; HTTPS/TLS reverse proxy, güvenilir forwarded
headers ayarları ve Data Protection anahtarlarının kalıcı/güvenli saklanması
operatör sorumluluğudur. Bu dosyalar otomatik üretim deployment'ı değildir.
Yerel Docker daemon kapalı olduğu için image build/run bu teslimde çalıştırılmadı;
Compose yapılandırması docker compose config --quiet ile doğrulandı.

## Health

- GET /: servis adı, sürüm ve geliştirme OpenAPI bağlantısı.
- GET /health/live: süreç çalışıyor mu; JSON 200.
- GET /health/ready: veritabanına erişilebiliyor ve bütün migration'lar uygulanmış mı.
- Readiness başarılıysa 200 Healthy, aksi halde 503 Unhealthy düz metin döner.
- OpenAPI yalnızca Development'ta /openapi/v1.json adresindedir.
- Hata cevaplarında ProblemDetails kullanılır; traceId log ilişkilendirmesi içindir.

## Migration ve CI

Başlangıçta otomatik migration yapılmaz. Yalnızca projeye ayrılan veritabanını
hedefleyin. Üretimde yedek ve gözden geçirilmiş migration planı kullanın.

```powershell
dotnet ef migrations has-pending-model-changes --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release
dotnet ef migrations script --idempotent --project ECommerce.Infrastructure --startup-project ECommerce.Api --configuration Release --output .artifacts/migrations.sql
```

.github/workflows/ci.yml push/pull request sonrası restore/audit, Release build,
model snapshot ve geçici PostgreSQL'de bütün migration'ları denetler. CI'deki parola
sadece geçici runner veritabanına aittir; gerçek bir hesap/parola değildir.
Workflow henüz push edilmediği için GitHub üzerinde çalıştırılmış sayılmaz.
Kalıcı otomatik davranış testi projesi yoktur; bu iş akışı onun yerine geçmez.

## İşlevsel teslim kontrolü

- Admin kategori, ürün, ürüne özel seçenek, varyant, fiyat ve stok yönetebilir.
- Admin ürünü pasifleştirip yeniden açabilir.
- Customer kayıt/giriş, parola değişikliği ve token yenileme yapabilir.
- Customer yalnızca kendi sepet ve siparişlerine erişebilir.
- Sipariş fiyatı/adresi anlık kopyadır; stok ve sepet tek transaction'da güncellenir.
- İdempotency ve çift iptal koruması vardır.
- Admin onay/kargo/teslim durumlarını yönetebilir.
- TRY/USD ayrı fiyat ve ayrı sipariş para birimi olarak çalışır.

## Üretim sınırları

Gerçek ödeme, kargo sağlayıcısı, vergi/kargo ücret motoru, e-posta doğrulama,
parola kurtarma teslimatı, MFA, iade/refund ve çok satıcılı yapı kapsam dışındadır.
Kalıcı otomatik testler, güvenlik incelemesi, TLS/secret/anahtar yönetimi,
izleme ve yedekleme üretim öncesi tamamlanmalıdır. Bu proje bir öğrenme/portföy
backend MVP'sidir; para tahsil eden production-ready sistem olarak sunulmaz.
