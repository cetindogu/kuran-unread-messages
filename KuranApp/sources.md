# Kuran Uygulaması Kaynak Listesi

Bu uygulama aşağıdaki kaynaklardan beslenmektedir:

- **Diyanet Kuran Portalı**: [kuran.diyanet.gov.tr](https://kuran.diyanet.gov.tr) - Arapça metin ve Türkçe meal.
- **AlQuran.cloud API (tr.diyanet)**: [api.quran.com](https://api.quran.com) - Ayetlerin Türkçe meali için birincil kaynak olarak Diyanet İşleri Başkanlığı meali (`tr.diyanet`) kullanılmaktadır.
- **Quran.Foundation API**: [api.quran.com](https://api.quran.com) - Ayet, meal ve tefsir verileri.
- **AlQuran.cloud API**: [alquran.cloud](https://alquran.cloud) - Kur'an metni ve çeşitli dillerdeki tercümeler.
- **Tefsir Kaynakları**:
    - Elmalılı Hamdi Yazır Meali
    - Diyanet Vakfı Meali
    - İbn Kesir Tefsiri

# Teknik Referanslar
- **Munâsebet İlmi**: Ayetler arası bağlantılar için akademik referanslar.
- **Sıralama Mantığı (Ordering Logic)**:
    - **Mushaf Sırası (Mushaf Order)**: Kur'an'ın kitap halindeki mevcut düzeni (örn. Fatiha 1. suredir). `SurahId` ve `VerseNumber` alanları bu sırayı temsil eder.
    - **Nüzul Sırası (Revelation Order)**: Ayetlerin vahiy ediliş zamanına göre kronolojik sırası (örn. Alak Suresi ilk inen suredir). `RevelationOrder` alanı bu sırayı temsil eder.
- **Kullanıcı Bazlı Takip (User-Based Tracking)**: Okundu bilgisi artık `Verse` tablosunda değil, `UserReads` tablosunda kullanıcı bazlı olarak tutulur. Bu sayede farklı kullanıcılar kendi okuma süreçlerini bağımsız olarak takip edebilir.
- **İndirilme Sırası (Download Order)**: Ayetler sisteme kaydedilme anlarına göre (`DownloadedAt`) izlenir. Varsayılan listeleme artık `RevelationOrder` (Nüzul Sırası) üzerinden yapılarak Kur'an'ın iniş süreci simüle edilir.
- **Microsoft.Data.Sqlite**: SQLite veritabanı erişimi için.
- **ASP.NET Core Web API**: Backend mimarisi.
