# Sepet

## Neden ayrı bir Cart modeli?

ProductVariant satın alınabilen ürünü, Cart ise müşterinin seçimini temsil eder.
Sepet satırında yalnızca varyant kimliği ve adet tutulur. Fiyat, isim, SKU, seçili
seçenekler ve mevcut stok katalogdan sunucuda okunur. Böylece istemcinin gönderdiği
fiyat veya toplamla ödeme hesabı yapılmaz.

Siparişten farklı olarak sepet fiyatı sabitlenmez. Yönetici fiyat değiştirdiğinde
sonraki sepet okuması güncel fiyatı gösterir. Sipariş oluştururken fiyat ve stok
yeniden doğrulanmalı, sipariş satırlarına anlık kopyalar alınmalıdır; sipariş
endpoint'i /api/orders altında uygulanmıştır.

## Sahiplik ve roller

- Her müşterinin en fazla bir kalıcı sepeti vardır.
- Bütün sepet endpoint'leri Customer rolünü gerektirir.
- Kullanıcı kimliği ICurrentUser üzerinden doğrulanmış token claim'inden alınır.
- İstek gövdesi veya query içindeki userId/cartId dikkate alınmaz.
- Başkasının sepetini kimliğiyle okuyacak veya değiştirecek endpoint yoktur.
- Sadece Admin rolüne sahip bootstrap hesabı sepet kullanamaz; alışveriş için
  ayrı bir Customer hesabı oluşturun.
- Anonim istek 401, Customer rolü olmayan giriş yapmış kullanıcı 403 alır.

## Para birimi

Bir sepet TRY veya USD taşır; iki para birimi aynı toplamda karışmaz.
Kur dönüşümü yapılmaz. Sepet boşken para birimi değiştirilebilir. Sepet doluyken
farklı para birimine geçmek veya farklı para birimiyle ürün eklemek 409 döndürür.
Aynı para birimini tekrar seçmek serbesttir.

Ürünün seçilen para biriminde fiyatı yoksa ekleme/güncelleme reddedilir. TRY fiyatı
USD yerine kullanılamaz. Mevcut sepet satırının fiyatı sonradan bulunamazsa birim
fiyat, satır toplamı ve sepet toplamı null döner; eksik fiyat sıfır sayılmaz.
Geçerli sıfır fiyat ise desteklenir.

CurrencyCode.Normalize, katalog fiyatları ve sepette ortak para birimi kontrolüdür.

## Adet ve stok

- Aynı varyant tekrar eklenirse yeni satır yerine adet artırılır.
- Bir satır 1-999 adet içerebilir; toplamda en fazla 100 farklı varyant olabilir.
- Adedi sıfıra indirmek yerine DELETE kullanılır.
- Ekleme ve adet güncelleme anında mevcut fiyat ve stok kontrol edilir.
- Sepete eklemek stok azaltmaz veya rezerve etmez.
- Stok sonradan azalırsa satır insufficient_stock işaretlenir.
- Müşteri adedi azaltabilir veya satırı silebilir.

CanCheckout yalnızca o okuma anında sepetin dolu ve bütün satırların fiyat/stok
açısından uygun olduğunu belirtir. Stok garantisi, ödeme onayı veya tamamlanmış
sipariş değildir. Başka müşteriler aynı stoğu sepetlerine ekleyebilir; asıl stok
yarışı sonraki sipariş transaction'ında ele alınacak.

## Endpoint'ler

| HTTP | Yol | Sonuç |
|---|---|---|
| GET | /api/cart | Güncel fiyat/stok içeren kendi sepetin |
| POST | /api/cart/items | Varyant ekle veya adedini artır |
| PUT | /api/cart/items/{variantId} | Adedi mutlak değerle belirle |
| PUT | /api/cart/currency | Boş sepetin para birimini belirle |
| DELETE | /api/cart/items/{variantId} | Kendi sepetinden satırı sil |
| DELETE | /api/cart | Bütün satırları sil, para birimini koru |

GET ve başarılı ekleme/güncelleme 200; DELETE işlemleri 204 döndürür.
Olmayan satırı silmek veya olmayan sepeti temizlemek idempotenttir.
Olmayan satırın adedini değiştirmek 404 döndürür.

Henüz sepet yoksa GET veritabanına kayıt yazmadan şu sonucu verir:

```json
{
  "id": null,
  "currency": "TRY",
  "items": [],
  "totalAmount": 0,
  "canCheckout": false
}
```

İlk başarılı ekleme veya para birimi seçimi kalıcı sepeti oluşturur.
Başarısız ilk ekleme boş bir sepet kaydı bırakmaz.
POST aynı istek tekrar gönderilince adet artırır; ağ hatasında sonucu belirsiz
olan isteği körlemesine tekrar göndermek yerine sepeti okuyup kontrol edin.
PUT mutlak adet belirler; aynı PUT tekrarı adedi yeniden artırmaz.

## Görünüm ve hata alanları

Satırda ProductVariantId, ProductId, ProductName, Sku, Quantity, UnitPrice,
LineTotal, AvailableStock, Options, IsAvailable ve Issue alanları bulunur.
JSON alanları camelCase olarak gönderilir.

Issue değerleri:
- insufficient_stock: istenen adet mevcut stoktan fazla.
- price_unavailable: sepetin para biriminde fiyat yok.
- variant_unavailable: varyant okunamıyor; ileriye dönük savunmacı görünüm.
- product_inactive: yönetici ürünü satıştan kaldırmış; satır silinebilir ama satın alınamaz.

Normal akışta foreign key kısıtları sepetteki varyantın fiziksel silinmesini engeller.
CanCheckout=false olan satırlar otomatik silinmez; müşteri problemi görebilir.

## Kalıcılık ve eşzamanlılık

Carts.UserId benzersizdir. CartItems için cart/variant ikilisi benzersizdir.
Adet ve para birimi veritabanında check constraint ile de korunur. Kullanıcı ve
varyant ilişkileri foreign key ile doğrulanır.

Cart aggregate'ının Revision alanı Infrastructure'da concurrency token olarak
eşlenir. Satır değişikliklerinde de kök Revision yenilenir. Aynı sepeti okuyup
birbiriyle çakışan değişiklik yapan isteklerden biri 409 alır. Başarısız değişiklik
ve alt satır güncellemesi birlikte geri alınır. İlk sepet oluşturma yarışında da
benzersiz kullanıcı indeksi ikinci sepeti engeller; istemci güncel sepeti okumalıdır.

Ürün bilgileri satır başına ayrı sorgu yerine toplu okunur. Fiyat, seçenek ve stok
okumaları repeatable-read transaction içinde aynı katalog görünümünü kullanır.
Sepetin kendisiyle katalog okuması arasında değişiklik olabilir; bu görünüm
checkout fiyat/stok garantisi değildir.

EF Core'un concurrency ve transaction davranışı:
[Concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency),
[Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

## Doğrulama

Geçici PostgreSQL 18 örneğinde 63 HTTP kontrolü ve 22 domain/veritabanı kontrolü
geçti. Sahte fiyat/kullanıcı kimliği, başka müşterinin sepetine erişim, TRY/USD
ayrımı, eksik fiyat, stok azalması, adet sınırları, satır silme, kök concurrency
ve benzersiz/check constraint'ler doğrulandı. Önceki 16 katalog domain kontrolü
tekrar geçti. Bunlar geçici kontrollerdir; kalıcı test projesi veya CI testi yoktur.
Kullanıcının gerçek veritabanı değiştirilmedi.
