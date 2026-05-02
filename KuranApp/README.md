# KuranApp Backend (API)

Uygulamanın veri yönetimi, senkronizasyonu ve kullanıcı takibinden sorumlu olan .NET 10 Web API servisidir.

## 🛠️ Teknik Yetenekler
- **Veri Senkronizasyonu:** AlQuran API'den Arapça ve Türkçe verileri otomatik çeker ve yerel JSON dosyalarında saklar.
- **Hızlı Persistence:** JSON verilerini SQLite veritabanına nüzul sırasını koruyarak hızlıca aktarır.
- **Resumable Sync:** Yarıda kalan indirme işlemlerini algılar ve kaldığı yerden devam eder.
- **CORS Desteği:** Güvenli cross-origin desteği ile Flutter Web istemcilerine hizmet verir.
- **Güvenli Veritabanı:** SQLite tabanlı, yabancı anahtar kısıtlamalarına sahip ilişkisel veri yapısı.

## 📡 API Uç Noktaları
- `GET /Surahs`: Tüm sureleri nüzul sırasına ve kullanıcı okuma durumuna göre getirir.
- `GET /Surahs/{number}/verses`: Belirli bir surenin tüm ayetlerini getirir.
- `POST /Surahs/{id}/markread`: Bir sureyi kullanıcı için "okundu" olarak işaretler.
- `POST /Verses/sync`: API verilerini günceller ve veritabanını yeniden yapılandırır.

## 💾 Teknolojiler
- .NET 10
- Microsoft.Data.Sqlite
- System.Text.Json
