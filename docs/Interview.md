# Projeyi anlatırken

Ezber bir mimari tanımı yerine, koddan bir örnek açıp kararını anlat.

- Dört katman neden var? Domain iş kurallarını, Application kullanım senaryolarını
  tutuyor. EF Core ve Identity Infrastructure'da; HTTP ve rol kontrolleri API'de.
  Bir fiyat kuralını değiştirmek için controller'a gitmemelisin.
- CQRS burada ne demek? Okuma ve yazma isteklerini ayrı handler'larla ele alıyoruz.
  İkisi de aynı PostgreSQL veritabanını kullanıyor. Ayrı bir okuma sistemi yok.
  MediatR isteği handler'a iletiyor; iş kuralının kendisi değil.
- Repository ve UnitOfWork neden tutuldu? Handler'ların ihtiyacı olan sorgular için
  arayüzler var. DbContext UnitOfWork'ü uyguluyor. Checkout gibi çok adımlı işlemler
  ayrıca açık transaction kullanıyor. Yeni bir genel repository katmanı eklemedik.
- Son ürünü iki kişi alırsa ne olur? Checkout stok satırlarını kilitliyor ve stoğu
  transaction içinde tekrar kontrol ediyor. İlk işlemden sonra diğer müşteri için
  stok kalmadıysa 409 dönüyor. Bunun PostgreSQL üzerinde eşzamanlı testi var.
- Revision ne işe yarıyor? Eski veriyle yazmayı fark ettiren optimistic concurrency
  token'ı. Satır kilitlerinin yerine geçmiyor. İlgisiz bir aggregate okununca onun
  Revision'ını değiştirmemek için SaveChanges davranışını düzelttik.
- Aynı sipariş isteği tekrar gelirse? Müşteriye ait idempotency anahtarı aynı
  siparişi döndürüyor. Aynı anahtarla farklı içerik gönderilirse 409 dönüyor.
  Idempotency, stok kilidi ve transaction farklı sorunları çözüyor.
- Fiyatı istemci belirleyebilir mi? Hayır. Sunucu fiyatı veritabanından alıyor.
  expectedTotal sadece müşterinin gördüğü toplamı doğruluyor. Sipariş satırları
  fiyat ve ürün bilgilerini saklıyor; katalog değişince geçmiş sipariş değişmiyor.
- Neden PostgreSQL testleri var? In-memory testler gerçek satır kilitlerini,
  transaction davranışını ve benzersizlik kısıtlarını doğrulamaz. Testler kendi
  şemalarını oluşturuyor; mevcut uygulama tablolarını sıfırlamıyor.

Gösterilecek kod: CheckoutHandler, ECommerceDbContext ve CheckoutTests.
Önce başarılı akışı, sonra son stok yarışı ve tekrar sipariş testini anlat.

Eksikleri de açıkça söyle: ödeme/kargo entegrasyonu yok; sipariş kaydı ödeme
alındığı anlamına gelmiyor. Canlıya çıkış için TLS, anahtar saklama, yedekleme ve
izleme gerekiyor. Bu bir portföy MVP'si, üretim sistemi iddiası değil.
