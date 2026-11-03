# Sipariş ve stok

## Müşteri akışı

1. Customer hesabıyla giriş yapın ve sepeti doldurun.
2. GET /api/cart ile güncel currency ve totalAmount değerlerini okuyun.
3. Yeni bir GUID Idempotency-Key üretin.
4. POST /api/orders isteğinde expectedCurrency, expectedTotal ve shippingAddress gönderin.
5. Ağ hatası halinde aynı istek gövdesi ve aynı anahtarla tekrar deneyin.

Başarılı istek 201 ve Pending siparişi döndürür; sepet temizlenir. Aynı anahtarla
tekrar istek aynı sipariş kimliğini döndürür; yeni sipariş veya stok düşme yapılmaz.
Yanıt mevcut sipariş durumunu içerir; ilk HTTP yanıtının birebir önbelleği değildir.
Anahtar müşteri kapsamında benzersizdir. Aynı anahtar farklı adres veya beklenen
tutarla kullanılırsa 409 döner. Başarısız transaction anahtarı tüketmez.

Fiyat değişirse sessizce yeni fiyat kabul edilmez. 409 sonrası sepet yeniden
okunmalı ve müşteri güncel toplamı onaylamalıdır. Sunucu fiyatları daima katalogdan
alır; expectedTotal yalnızca müşteri onayını karşılaştırmak içindir.

## API

Customer:
- POST /api/orders: sipariş oluştur; Idempotency-Key başlığı zorunlu GUID.
- GET /api/orders: yalnızca kendi siparişlerinin sayfalı listesi.
- GET /api/orders/{id}: kendi sipariş detayı; başkasının kimliği 404 döner.
- POST /api/orders/{id}/cancel: Pending veya Confirmed siparişi iptal et.

Admin:
- GET /api/admin/orders: bütün siparişlerin sayfalı listesi.
- GET /api/admin/orders/{id}: adres ve satır kopyalarıyla detay.
- POST /api/admin/orders/{id}/confirm
- POST /api/admin/orders/{id}/ship
- POST /api/admin/orders/{id}/deliver
- POST /api/admin/orders/{id}/cancel

Listelerde status, page, pageSize kullanılabilir; pageSize en fazla 100'dür.
Durum akışı Pending -> Confirmed -> Shipped -> Delivered şeklindedir.
Pending ve Confirmed -> Cancelled geçişi yapılabilir. Shipped/Delivered iptal
edilemez; iade/refund sistemi bu MVP'nin dışında. Tekrarlanan aynı durum işlemi
zararsızdır; geriye geçiş reddedilir.

## Kopyalar ve para

Ürün adı, SKU, seçenek özeti, adet, birim fiyat, para birimi ve teslimat adresi
sipariş anında kopyalanır. Sonradan katalog değişse de geçmiş sipariş değişmez.
Tutarlar decimal olarak tutulur. TRY ve USD ayrı fiyatlardır; kur dönüşümü yoktur.
TotalAmount ürün toplamıdır; ayrı vergi/kargo ücreti hesaplama motoru yoktur.

Pending ve Confirmed ödeme alınmış anlamına gelmez. Gerçek ödeme alınmaz,
bankaya veya kargo şirketine istek gönderilmez. Yönetici ship/deliver işlemleri
bu MVP'de manuel durum yönetimidir.

## Transaction ve yarışlar

Sipariş oluşturma, bütün stok azaltmaları ve sepet temizliği tek transaction'dır.
Herhangi bir hata bütün değişiklikleri geri alır.

- Müşteri bazlı PostgreSQL transaction advisory lock aynı anda gelen checkout
  isteklerini sıraya alır; kilit transaction sonunda kendiliğinden bırakılır.
- Sepet satır kilidiyle okunur.
- Etkilenen ürün ve varyant satırları kimlik sırasıyla FOR UPDATE kilitlenir.
- Fiyat/stok kontrolü kilitler alındıktan sonra yapılır.
- Ürün, sepet ve sipariş Revision alanları da eşzamanlı değişimi denetler.
- Cancel sipariş satırını kilitler ve sadece ilk başarılı iptalde stok iade eder.
- Veritabanı kullanıcı/anahtar ikilisinde unique index uygular.

Deadlock veya eşzamanlı güncelleme 409 dönebilir; otomatik kör retry yapılmaz.
Güncel veri okunup aynı idempotency anahtarıyla tekrar denenebilir. Transaction
içinde dış servis çağrısı yapılmaz. İptalde stok int kapasitesini aşacaksa işlem
geri alınır; yönetici stok bilgisini düzeltmeden kısmi iade yapılmaz.

Kaynaklar:
[PostgreSQL kilitleri](https://www.postgresql.org/docs/current/explicit-locking.html),
[EF Core transaction](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

## Doğrulama

Checkout, son stok yarışı, çift istek/iptal, sahiplik ve fiyat kopyaları
kalıcı PostgreSQL testlerinde çalıştırılır. Komutlar: [Testing.md](Testing.md).
