# Mimariyi açıklama rehberi

## Dört katman

Domain iş kurallarını taşır; EF Core, MediatR veya HTTP bilmez.
Application kullanım senaryolarını command/query handler'larıyla yürütür.
Infrastructure arayüzleri EF Core, PostgreSQL ve Identity ile uygular.
Api model binding, kimlik doğrulama, rol koruması ve HTTP cevaplarını yönetir.

Api -> Application + Infrastructure, Infrastructure -> Application + Domain,
Application -> Domain bağımlılıkları vardır. Repository'ler her tabloyu generic
CRUD'a sarmalamak yerine ürün, sepet ve sipariş kullanımına göre tanımlanır.

## Aggregate sınırları

Product seçenek ve varyant kombinasyonlarını korur. Cart kendi satırlarını ve
tek para birimi kuralını korur. Order geçmiş veri kopyalarını ve durum geçişlerini
korur. Koleksiyonlar dışarıdan değiştirilemez; public setter yerine davranış
metotları kullanılır.

ProductVariant fiyatı satılabilir kombinasyona aittir. Renk/Beden sabit property
değildir; ProductOption/ProductOptionValue ile ürüne özel tanımlanır. Varyant
tam olarak her seçenekten bir değer içerir. Tekrarlanan kombinasyon engellenir.
Seçeneksiz üründe de tek bir varsayılan varyant vardır.

## CQRS ve MediatR

CQRS burada komut ve sorguların ayrı handler'larda olmasıdır; ayrı veritabanı veya
event sourcing yoktur. MediatR HTTP isteğini uygun handler'a yönlendirir.
CQRS, MediatR olmadan da uygulanabilir. Okuma işlemleri DTO üretir; entity'ler
doğrudan istemciye gönderilmez.

## Transaction ve eşzamanlılık

Repository sorguları/veri erişimini, IUnitOfWork kaydetmeyi soyutlar.
Checkout birden fazla aggregate değiştirdiği için açık transaction kullanır.
Satır kilitleri stok yarışını, Revision tokenları da stale aggregate yazılarını
denetler. SQL unique/check/FK kısıtları uygulama doğrulamalarını tamamlar.

Önce SELECT ile var mı diye bakmak tek başına benzersizlik garantisi vermez:
iki istek aynı anda yok sonucunu görebilir. Bu yüzden SKU, kombinasyon, sepet
sahibi ve idempotency anahtarı gibi alanlarda veritabanı indeksleri de gerekir.

## Kimlik ve sahiplik

Customer/Admin role kontrolü API'de uygulanır. Sipariş handler'ları da rolü
denetler. ICurrentUser doğrulanmış claim'den kullanıcı kimliğini sağlar.
Kullanıcı kimliği istek gövdesinden alınmaz. Identity parola hash'ini yönetir;
uygulama kendi parola kriptografisini yazmaz. Access tokenlar JWT değil, Identity
opak tokenlarıdır. Ayrıntılı süre/iptal davranışı Identity.md içinde belgelenmiştir.

## Sepet ile sipariş neden farklı?

Sepet bir niyettir: stok ayırmaz, fiyatı güncel okur. Order satın alma kaydıdır:
isim, SKU, seçenek, fiyat ve adresi kopyalar. Ürün sonradan değişse bile geçmiş
sipariş aynı kalır. expectedTotal sunucuya fiyat dayatmaz; müşterinin gördüğü
tutarın değişmediğini kontrol eder.

İdempotency aynı işlemin ağ yüzünden tekrar gönderilmesine karşıdır. Aynı
anahtarlı başarılı checkout tek sipariş üretir. İptal de yalnızca ilk seferde
stok iade eder. Bunlar frontend'de butonu kapatmakla çözülemez.

## Sadelik sınırı

Mikroservis, event bus, event sourcing, ayrı read database veya gereksiz generic
altyapı yoktur. Gerçek ödeme/kargo eklendiğinde dış servis yan etkileri transaction
içine konulmamalı; ödeme durumları, outbox/idempotency ve hata telafisi ayrıca
tasarlanmalıdır. Şimdiki Confirmed durumunu Paid olarak anlatmamak önemlidir.
