# Proje kapsamı

Bu proje tek mağazalı bir .NET backend portföy uygulamasıdır.

## Tamamlanan işlevler

- Ürüne özel seçenekler, varyant kombinasyonları ve SKU yönetimi.
- Bağımsız TRY/USD fiyatları; kur dönüşümü yok.
- Identity kayıt/giriş, Customer/Admin yetkileri ve refresh token akışı.
- Müşteriye ait sepet; fiyat ve kullanıcı kimliği sunucuda belirlenir.
- Transaction içinde sipariş, stok düşme ve sepet temizleme.
- Müşteri kapsamlı idempotency anahtarı ve bir defalık iptal stoğu.
- Yönetici onay/kargo/teslim durumları ve ürün pasifleştirme.
- Kalıcı xUnit unit ve PostgreSQL entegrasyon testleri.

## Bilinçli sınırlar

Gerçek ödeme/kargo entegrasyonu, iade/refund, vergi motoru, MFA ve e-posta ile
hesap kurtarma kapsam dışındadır. Bu uygulama production-ready olarak sunulmaz.

Dört katman ve tek PostgreSQL veritabanı korunur. MediatR istek yönlendirmesinde,
repository'ler veri erişiminde, UnitOfWork transaction ve aggregate sürümlerinde
kullanılır. Mikroservis veya event sourcing eklenmez.

Kurulum: [Operations.md](Operations.md).
Mimari: [Architecture.md](Architecture.md).
Testler: [Testing.md](Testing.md).
Doğrulanan sonuçlar: [Validation.md](Validation.md).
