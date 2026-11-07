# Tasarım kararları

## Katmanlar

- **Domain**: Entity'ler ve iş kuralları. EF Core, MediatR veya HTTP'ye bağımlı değildir.
  Entity'lerin setter'ları private'tır; değişiklikler `AddVariant`, `DecreaseStock`, `Cancel` gibi metotlarla yapılır.
- **Application**: Her kullanım senaryosu bir MediatR handler'ıdır. Repository arayüzleri burada tanımlanır.
- **Infrastructure**: EF Core, PostgreSQL sorguları/kilitleri ve Identity.
- **Api**: Controller'lar, model doğrulama, rol kontrolü ve hataların HTTP cevabına çevrilmesi
  (`Errors/ApiExceptionHandler.cs`).

Bağımlılık yönü: Api → Application + Infrastructure → Domain. Okuma ve yazma işlemleri ayrı
handler'lardadır ama aynı veritabanını kullanır; ayrı bir okuma veritabanı yoktur.

## Ürün, seçenek ve varyant

Renk veya beden gibi özellikler sabit kolon değildir; her ürün kendi seçeneklerini
(`ProductOption`) ve değerlerini (`ProductOptionValue`) tanımlar. Satılabilir birim
`ProductVariant`'tır: SKU, stok ve fiyatlar varyanta aittir. Bir varyant her seçenekten
tam olarak bir değer içerir ve aynı kombinasyon iki kez oluşturulamaz. Seçeneksiz bir
üründe tek bir varsayılan varyant olur.

## Sepet ve sipariş neden ayrı?

Sepette sadece varyant kimliği ve adet tutulur. Fiyat, isim ve stok her okumada
katalogdan alınır; istemcinin gönderdiği fiyat kullanılmaz. Sepete eklemek stok ayırmaz.

Sipariş ise satın alma kaydıdır: ürün adı, SKU, seçenekler, birim fiyat ve adres sipariş
anında kopyalanır. Ürün sonradan değişse de geçmiş sipariş aynı kalır.

Bir sepet tek para birimi (TRY veya USD) taşır. Checkout isteğindeki `expectedTotal`
fiyatı belirlemez; sadece müşterinin gördüğü toplamın değişmediğini kontrol eder.
Fiyat değiştiyse 409 döner ve müşteri sepeti yeniden okur.

## Checkout ve stok

`CheckoutHandler` tek bir transaction içinde çalışır:

1. Müşteri için PostgreSQL advisory lock alınır; aynı müşterinin eşzamanlı checkout'ları sıraya girer.
2. Aynı `Idempotency-Key` ile daha önce sipariş oluşmuşsa o sipariş döner.
3. Sepet ve ilgili ürün/varyant satırları `SELECT ... FOR UPDATE` ile kilitlenir.
4. Fiyat ve stok kilit alındıktan sonra kontrol edilir, stok düşülür, sepet temizlenir, sipariş eklenir.

Herhangi bir adım hata verirse her şey geri alınır. Son stoğu iki müşteri aynı anda almaya
çalışırsa biri 201, diğeri 409 alır; bu durum integration testlerinde gerçek PostgreSQL ile denenir.

Idempotency anahtarı müşteri bazında benzersizdir (unique index). Aynı anahtar farklı
adres veya tutarla tekrar kullanılırsa 409 döner. İptal de sipariş satırını kilitler ve
stoğu yalnızca ilk iptalde iade eder.

## Eşzamanlı güncellemeler

Checkout dışındaki güncellemelerde optimistic concurrency kullanılır. PostgreSQL her
satır güncellemesinde `xmin` sistem kolonunu değiştirir; EF Core bu değeri UPDATE'in
WHERE koşuluna ekler. Satırı biz okuduktan sonra başka biri değiştirdiyse güncelleme
0 satırı etkiler, EF `DbUpdateConcurrencyException` fırlatır ve API 409 döner.

Önce SELECT ile "var mı" diye bakmak benzersizlik garantisi vermez, çünkü iki istek aynı
anda "yok" sonucunu görebilir. Bu yüzden SKU, varyant kombinasyonu, kullanıcı başına sepet
ve idempotency anahtarı için veritabanında unique index vardır.

## Kimlik ve yetki

Parola hash'leme, kilitleme (5 hatalı denemede 5 dakika) ve roller ASP.NET Core Identity ile
yapılır. Token'lar JWT değil, Identity'nin opak bearer token'larıdır (access 15 dakika,
refresh 7 gün). Çıkış ve parola değişikliği security stamp'i yeniler, böylece eski refresh
token'lar geçersiz olur.

Kullanıcı kimliği her zaman token'daki claim'den alınır (`ICurrentUser`), istek gövdesinden
alınmaz. Bir müşteri başkasının siparişini istediğinde 404 alır.

## Bilinçli olarak eklenmeyenler

Mikroservis, event bus, ayrı okuma veritabanı ve generic repository yoktur. Gerçek ödeme
eklenirse dış servis çağrısı veritabanı transaction'ı içine konmamalı; ödeme durumları
ayrıca tasarlanmalıdır. Şu anki `Confirmed` durumu "ödendi" anlamına gelmez.
