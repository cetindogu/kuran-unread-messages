# Kuran Nüzul Sırası Okuma Uygulaması

Bu proje, Kuran-ı Kerim'i indiriliş sırasına (Nüzul Sırası) göre okumanızı ve takip etmenizi sağlayan modern bir uygulamadır.

## 🌟 Temel Özellikler
- **Kronolojik Okuma:** 114 sureyi Alak suresinden başlayarak nüzul sırasına göre listeler.
- **Okuma Takibi:** Kullanıcılar tamamladıkları sureleri işaretleyerek ilerlemelerini kaydedebilir.
- **Çift Dil Desteği:** Ayetlerin Arapça orijinalleri ve Türkçe (Diyanet) mealleri yan yanadır.
- **Çoklu Platform:** Web, Android ve iOS platformlarında sorunsuz çalışır.
- **Akıllı Özetler:** AI destekli ayet özetleri ile derinlemesine anlama sağlar (Geliştirme aşamasında).

## 🏗️ Proje Yapısı
- **[KuranApp](./KuranApp):** .NET 10 tabanlı Web API ve SQLite veritabanı.
- **[kuran_mobile_app](./kuran_mobile_app):** Flutter ile geliştirilmiş cross-platform mobil ve web arayüzü.

## 🚀 Hızlı Başlangıç
1. Backend'i çalıştırın: `cd KuranApp && dotnet run`
2. Frontend'i çalıştırın: `cd kuran_mobile_app && flutter run`

---
*Bu proje Kuran'ı tarihi akışına göre anlamak isteyen okurlar için tasarlanmıştır.*
