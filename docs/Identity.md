# Identity ve yetkilendirme

## Tasarım kararları

ASP.NET Core Identity parola hashleme, kullanıcı doğrulama, roller, security stamp
ve hatalı giriş kilidini yönetir. Domain katmanına Identity bağımlılığı eklenmedi.
Application, IAccountService arayüzü ve MediatR handler'ları üzerinden hesap
işlemlerini yürütür. Framework'ün token üretme ve doğrulama kısmı API sınırındadır.

UserId, ICurrentUser üzerinden doğrulanmış kullanıcının claim'inden alınır;
profil işlemleri istemcinin gönderdiği bir kullanıcı kimliğine güvenmez. Aynı
yaklaşım sonraki sepet ve sipariş sahipliği kontrollerinde kullanılacak.

## Roller

- Customer: herkese açık kayıt endpoint'i yalnızca bu rolü verir.
- Admin: katalog yazma işlemlerine erişir.
- Katalog okumak için giriş gerekmez.
- Roller AddIdentity migration'ıyla oluşturulur; normal başlangıçta rol veya
  yönetici hesabı otomatik oluşturulmaz.
- İstek gövdesine role/roles eklemek yönetici yetkisi vermez.
- EmailIndex benzersizdir; eşzamanlı kayıt isteklerine karşı veritabanı koruması vardır.

## İlk yönetici

Önce bağlantı bilgisini README'deki gibi User Secrets'a ekleyin ve migration'ları
uygulayın. Sonra kendi değerlerinizle:

```powershell
dotnet user-secrets set "AdminBootstrap:Email" "YONETICI_EPOSTANIZ" --project ECommerce.Api
dotnet user-secrets set "AdminBootstrap:Password" "GUCLU_BENZERSIZ_PAROLANIZ" --project ECommerce.Api
dotnet run --project ECommerce.Api --configuration Release --launch-profile http -- --bootstrap-admin
dotnet user-secrets remove "AdminBootstrap:Password" --project ECommerce.Api
dotnet user-secrets remove "AdminBootstrap:Email" --project ECommerce.Api
```

Yer tutucuları gerçek, geçerli e-posta ve politika koşullarını sağlayan parola ile
değiştirin. İlk komutlar shell geçmişine yazılabilir; ortak terminalde kullanmayın.
User Secrets geliştirme kolaylığı sağlar; şifrelenmiş bir üretim secret kasası değildir.

Bootstrap yalnızca açıkça verilen --bootstrap-admin bayrağıyla çalışır, HTTP
sunucusu başlatmadan çıkar. Aynı yönetici tekrar verilirse parola değiştirilmez.
Aynı e-posta mevcut bir müşteriye aitse işlem reddedilir; hesap sessizce yöneticiye
yükseltilmez. Normal uygulama başlangıcında bootstrap değerleri kullanılmaz.
Üretimde AdminBootstrap__Email ve AdminBootstrap__Password güvenli secret
yönetimiyle sağlanabilir. Komutun rol tabloları için migration uygulanmış olmalıdır.

## Endpoint'ler

| HTTP | Yol | Erişim |
|---|---|---|
| POST | /api/auth/register | Herkese açık; Customer oluşturur |
| POST | /api/auth/login | Herkese açık; token üretir |
| POST | /api/auth/refresh | Geçerli refresh token gerekir |
| GET | /api/auth/me | Giriş yapmış kullanıcı |
| POST | /api/auth/change-password | Giriş yapmış kullanıcı ve mevcut parola |
| POST | /api/auth/logout | Giriş yapmış kullanıcı; refresh oturumlarını iptal eder |

Kayıt 201 ve id/email/roles döndürür. Giriş ve refresh yanıtı:

```json
{
  "tokenType": "Bearer",
  "accessToken": "TOKEN",
  "expiresIn": 900,
  "refreshToken": "REFRESH_TOKEN"
}
```

Korumalı isteklere Authorization: Bearer ACCESS_TOKEN başlığını ekleyin.
Tokenlar JWT değil, ASP.NET Core Identity'nin Data Protection ile koruduğu
opak tokenlardır. Bu sürüm yalnızca bearer kullanır; cookie girişi yoktur.
Tarayıcıya özel frontend eklendiğinde cookie/BFF ve CSRF yaklaşımı ayrıca
değerlendirilmelidir; tokenları gelişigüzel localStorage'a koymayın.

## Süreler ve iptal davranışı

- Access token 15 dakika, refresh token 7 gün geçerlidir.
- Refresh sırasında security stamp, kullanıcı varlığı ve hesap kilidi denetlenir.
- Refresh yeni principal oluşturur; güncel roller kullanılır.
- Logout bütün refresh oturumlarını security stamp değiştirerek iptal eder.
- Parola değişikliği de eski refresh tokenlarını geçersiz kılar.
- İstemci logout sonrasında kendi access/refresh tokenlarını silmelidir.
- Önceden verilmiş access tokenlar süreleri dolana kadar geçerli kalabilir;
  her istekte veritabanından token iptali kontrol edilmiyor.
- Refresh token tek kullanımlık değildir; otomatik reuse detection uygulanmadı.

Bunlar bilerek belgelenmiş MVP sınırlarıdır; anlık bütün-token iptali veya tek
kullanımlık refresh istenirse sunucu taraflı oturum saklama tasarlanmalıdır.

## Giriş koruması

Parola 10-128 karakter, en az bir büyük harf, küçük harf, rakam ve özel karakter
içermelidir. Parola plaintext saklanmaz; doğrulama Identity tarafından yapılır.

Beş başarısız girişten sonra hesap beş dakika kilitlenir. Kilitli hesap refresh
yapamaz. Yanlış parola ve bulunamayan kullanıcı aynı 401 durumuyla yanıtlanır.

Register/login/refresh toplamında IP başına dakikada 60 istek sınırı vardır.
Sınır aşılırsa 429 döner. Sınır süreç belleğindedir; çok sunuculu dağıtımda ortak
rate limit altyapısı ve güvenilir reverse proxy ayarları ayrıca gereklidir.

## Üretim öncesi açık işler

E-posta doğrulama ve parola kurtarma teslimatı, MFA, TLS/reverse proxy ayarları,
güvenli Data Protection anahtar saklama, yedekleme ve kalıcı otomatik testler
tamamlanmalıdır. Data Protection anahtarları kaybolursa tokenlar okunamaz;
birden çok instance aynı uygulama adı ve güvenli paylaşılan anahtar deposunu
kullanmalıdır. Anahtarları repository'ye koymayın.

Bu sürümde e-posta doğrulaması zorunlu değildir. OpenAPI yalnızca Development
ortamında yayınlanır. .NET runtime ve NuGet paketleri güncel güvenlik yamalarıyla
kullanılmalıdır.

Kaynak:
https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization

## Doğrulama kaydı

Geçici PostgreSQL 18 veritabanında 49 Identity HTTP kontrolü ve yönetici tokenıyla
35 katalog regresyon kontrolü geçti. Production ortamında anonim yazmanın 401,
müşteri yazmasının 403, yönetici yazmasının 201 ve OpenAPI erişiminin 404 döndüğü
doğrulandı. Altı eşzamanlı kayıt aynı e-posta için tek Customer hesabı ve beş 409
yanıtı üretti. Bootstrap'in mevcut müşteriyi yöneticiye yükseltmediği kontrol edildi.
Eski katalog verisi Identity migration'ından sonra korunmuştu.

Kontroller geçici program ve HTTP istekleriyle yapıldı; kalıcı otomatik test
projesi veya CI testi olarak sunulmuyor. Kullanıcının asıl veritabanı değiştirilmedi.
