# Son doğrulama kaydı

## Ortam ve kapsam

.NET SDK 10.0.101, .NET 10 hedefi ve ayrı, geçici PostgreSQL 18 örneği kullanıldı.
Asıl kullanıcı veritabanına migration veya veri yazılmadı. Kontroller Release
çıktısında ve localhost HTTP üzerinden çalıştırıldı. Açık Debug süreci kapatılmadı.

Boş veritabanına önce InitialCatalog uygulandı, eski kategori/ürün örneği eklendi,
sonra kalan migration zinciri tamamlandı. Eski ürün aktif varsayılanıyla korundu.
Pending migration varken readiness 503, tamamlandıktan sonra 200 döndü.
Veritabanı kapatıldığında readiness 503, liveness 200 davranışı da doğrulandı.
Production ortamında müşteri/yönetici rol sınırları ve OpenAPI'nin kapalı olması kontrol edildi.

## Tekrarlanan kontrol grupları

| Grup | Geçen kontrol |
|---|---:|
| Identity HTTP ve yetkilendirme | 49 |
| Sepet HTTP ve sahiplik | 63 |
| Sipariş HTTP ve gerçek eşzamanlı istekler | 58 |
| Ürün pasifleştirme ve health | 27 |
| Sipariş domain/stock sınırları | 27 |
| Sepet domain ve PostgreSQL kalıcılık kısıtları | 22 |
| Önceki katalog domain kontrolleri | 16 |

Bu sayılar assertion/kontrol sayılarıdır; repository'de bulunan bir test
framework'ünün test case sayıları değildir. Geçici kontrol programları kaynak
koduna veya CI test projesi olarak eklenmedi. Kalıcı otomatik davranış testleri
henüz yoktur ve bu teknik borç açıkça korunmaktadır.

## Kritik senaryolar

- Son tek stok için iki müşteri yarıştığında tek sipariş başarılı oldu.
- Aynı müşteri/anahtarla eşzamanlı checkout aynı siparişi döndürdü.
- Aynı sipariş için iki eşzamanlı iptal stoğu bir defa iade etti.
- Stok/fiyat hatasında sepet ve bütün stok satırları korundu.
- Ürün/fiyat değişiklikleri eski sipariş kopyalarını değiştirmedi.
- Pasif ürün gizlendi ve satın alınamadı; geçmiş sipariş iptali yine çalıştı.
- Sahte userId/fiyat ve başka müşterinin sepet/sipariş kimliği yetki sağlamadı.
- TRY/USD karışmadı; eksik fiyat sıfır veya kur dönüşümü sayılmadı.
- Silinen sepet satırları reload sonrası geri gelmedi.
- Concurrency/unique/check constraint hataları veri bütünlüğünü korudu.

## Yapısal doğrulama

Release build warnings-as-errors ile başarılıydı. EF migration snapshot'ında
bekleyen model değişikliği bulunmadı. NuGet audit mevcut kaynaklara göre
kontrol edildi. Compose yapılandırması doğrulandı.

CI YAML sözdizimi ve salt okuma izinleri doğrulandı; remote workflow push edilmeden
çalıştırılmış sayılmaz. Release publish ve idempotent SQL migration çıktısı üretildi.
Docker daemon kapalı olduğundan image build/run yapılmadı. Container dosyaları
hazırlanmış ve Compose sözdizimi kontrol edilmiş olmakla birlikte Docker runtime
doğrulaması yapılmış gibi sunulmaz.

Bu kayıt production güvenlik sertifikası, yük testi, ödeme/kargo entegrasyon testi
veya kalıcı regresyon paketi değildir. İşlevsel MVP tesliminin kapsam kaydıdır.
