# ECommerce MVP tasarımı

## Onaylanan kararlar

- Tek satıcılı mağaza, PostgreSQL ve EF Core.
- Ürüne özel seçenekler ve yalnızca satılacak kombinasyonlar.
- TRY ve USD için yönetici tarafından ayrı girilen fiyatlar; otomatik kur dönüşümü yok.
- Application katmanında MediatR ile command/query ayrımı.
- Amaca özel repository arayüzleri ve tek veritabanı.
- Mikroservis, event sourcing ve ayrı okuma veritabanı kapsam dışında.

## Katalog modeli

Category -> Product -> ProductOption -> ProductOptionValue

Product -> ProductVariant -> ProductVariantOptionValue -> ProductOptionValue

ProductVariant -> ProductVariantPrice

Product, seçenek ve kombinasyon bütünlüğünün giriş noktasıdır. Seçenekler ve
varyantlar Product metotları üzerinden oluşturulur; koleksiyonlar dışarıdan
değiştirilemez. Varyant her seçenekten tam bir değer içermelidir. Seçeneksiz
ürünün tek varyantı vardır. Varyant oluşturulduktan sonra seçenek yapısı kilitlenir.

Bir varyant için aynı para biriminde tek fiyat tutulur. Fiyatlar negatif olamaz
ve en fazla iki ondalık basamak içerebilir. Bir para biriminde fiyat yoksa
ürün o para biriminde satın alınamaz; diğer para birimiyle sessizce değiştirilmez.
Fiyatsız varyant taslak olarak oluşturulabilir, fakat sipariş edilemez.

## Kalıcılık ve eşzamanlılık

- SKU mağaza genelinde normalize edilmiş benzersiz indeksle korunacak.
- Seçenek adı ürün içinde, değer adı seçenek içinde benzersiz olacak.
- Varyant ve para birimi ikilisi benzersiz olacak.
- Kombinasyon için sıralı değer kimliklerinden kararlı bir anahtar üretilecek;
  ürün kimliğiyle birlikte benzersiz indeks uygulanacak.
- Fiyat ve stok negatifliğine veritabanında da kısıt konulacak.
- Stok güncellemeleri eşzamanlılık kontrolüyle korunacak; yalnızca bellekte
  stok kontrolü yapıp kaydetmek yeterli değildir.
- Katalog yazma işlemleri tüm seçenek ve varyantları içeren ürünü yükleyecek.
- Sipariş geçmişi bulunan ürünler fiziksel silinmek yerine pasifleştirilecek.

## Application ve API

Özellik grupları: Categories, Products, Cart, Orders, Identity.
Her kullanım senaryosunun command veya query handler'ı olacak.
Repository arayüzleri Application'da, EF Core uygulamaları Infrastructure'da.
Domain HTTP, EF Core ve MediatR bağımlılığı taşımayacak.

Listeleme sorguları sayfalı DTO döndürecek. Yazma işlemleri domain davranışlarını
kullanacak. Api standart ProblemDetails hata yanıtları üretecek:
geçersiz istek 400, bulunamayan kaynak 404, çakışma 409.
Beklenmeyen hatalarda istemciye iç sistem ayrıntıları gönderilmeyecek.

## Kullanıcı, sepet ve sipariş

- ASP.NET Core Identity ile müşteri ve yönetici rolleri.
- Katalog yazma işlemleri yalnızca yöneticilere açık.
- Müşteri yalnızca kendi sepet ve siparişlerine erişebilir.
- Sepet satırı ProductVariant kimliği ve pozitif adet içerir.
- Sepetin ve siparişin tek para birimi vardır.
- İstemciden gelen fiyat güvenilir kabul edilmez.
- Sepete eklemek stok rezervasyonu değildir.
- Siparişte güncel fiyat ve stok sunucuda yeniden kontrol edilir.
- Sipariş satırında ürün adı, SKU, seçenek özeti, birim fiyat ve para birimi
  anlık kopya olarak saklanır.
- Sipariş oluşturma ve stok azaltma tek transaction içinde tamamlanır.
- Tekrarlanan sipariş isteği idempotency anahtarıyla korunur.
- İptal edilen siparişin stoğu yalnızca bir kez iade edilir.
- Gerçek ödeme ve kargo sağlayıcısı entegrasyonları bu MVP'nin dışında.
  Ödeme alınmadığı halde sipariş ödenmiş kabul edilmeyecek.

## Tamamlanma ölçütleri

1. Katalog domain modeli ve kural kontrolleri.
2. PostgreSQL eşlemeleri, indeksler, migration ve repository'ler.
3. Katalog command/query handler'ları ve HTTP endpoint'leri.
4. Kimlik doğrulama ve yönetici yetkilendirmesi.
5. Sepet, sipariş, stok ve iptal akışları.
6. PostgreSQL üzerinde entegrasyon testleri, API belgesi ve kurulum talimatları.

## Mevcut durum

İlk üç madde kapsamında katalog domain modeli, PostgreSQL migration/eşlemeleri,
repository'ler, MediatR command/query handler'ları ve katalog endpoint'leri eklendi.
Silme/pasifleştirme henüz uygulanmadı. Dördüncü madde kapsamında Identity ile
Customer/Admin rolleri, kayıt, giriş, token yenileme, profil, parola değiştirme
ve çıkış eklendi. Katalog yazma işlemleri her ortamda Admin rolü gerektirir.
Yönetici yalnızca ayrı bootstrap komutuyla oluşturulur; kayıt isteği rol seçemez.
Sepet ve sipariş açık işlerdir.

Geçici PostgreSQL 18 örneğinde 35 HTTP kontrolü, 4 kalıcılık/eşzamanlılık kontrolü
ve 2 üretim güvenlik sınırı kontrolü geçti. Kalıcı otomatik test projesi henüz yok.
Identity bölümünde 49 HTTP kontrolü ve mevcut müşterinin bootstrap ile yöneticiye
yükseltilemediği ayrıca doğrulandı. Eski katalog verisi migration sırasında korundu.
Önceki üretim 503 sınırı artık geçerli değildir; yeni davranış 401/403'tür.
Bu belge tamamlanmış MVP iddiası değil, kapsam ve ilerleme kaydıdır.
