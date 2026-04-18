# Uygulama Kaynakları ve Tefsirler  
Kod ajanın, her ayetin Arapça metni, Türkçe meali ve güvenilir tefsir bilgisini alabilmesi için öncelikle doğrulanmış kaynaklar belirlenmelidir. Örneğin, Diyanet’in resmi Kuran portalı Ayet metni (Arapça) ile birlikte Türkçe meal sunar【26†L105-L113】. Bunun yanı sıra **Quran.Foundation**’ın sağladığı içerik API’leri (Verses, Translations, Tafsirs) kullanılarak ayetlere ait çoklu meal ve tefsir kaynaklarına erişilebilir【28†L63-L70】【30†L229-L236】. AlQuran.cloud gibi açık API’lar da Kuran metni ile çeşitli çevirileri sağlar (örn. *quran-uthmani* Arapça; *en.asad* İngilizce meal)【7†L63-L71】. Bir kaynak listesi (örn. `sources.md`) oluşturularak kullanılacak API’ler ve çeviri/tefsir metinleri buraya eklenmelidir. Örneğin Diyanet Meali, Elmalılı Hamdi Yazır Meali gibi eski mealler veya güncel vaka için Diyanet Vakfı çevirisi de listelenebilir【29†L220-L228】【25†L186-L194】.  

# Veritabanı Tasarımı (SQLite)  
Her bir ayet için **Verses** tablosu oluşturulmalı; tipik alanlar: `SurahId` (sure no), `VerseNumber` (ayet no), `ArabicText`, `TurkishTranslation`, `Summary` (AI özeti), `IsRead` (okundu bayrağı), `DownloadedAt` (tarih) gibi. Okundu-durumu ve indirilme sırasını saklamak için `DownloadedAt` zamanı tutulabilir. Ayetler arasında çoklu bağlantı gerektiğinden, “çoktan çoğa” ilişkiyi temsil eden **VerseLinks** (ya da VerseRelations) adlı bir bağlantı tablosu kullanılmalıdır: bu tabloda `VerseId` ve `RelatedVerseId` alanları olacaktır【34†L100-L108】. Her iki sütun `Verses` tablosuna yabancı anahtar olabilir. Örneğin: 
```sql
CREATE TABLE Verses (
  Id INTEGER PRIMARY KEY,
  SurahId INTEGER,
  VerseNumber INTEGER,
  ArabicText TEXT,
  TurkishTranslation TEXT,
  Summary TEXT,
  IsRead BOOLEAN,
  DownloadedAt DATETIME
);
CREATE TABLE VerseLinks (
  Id INTEGER PRIMARY KEY,
  VerseId INTEGER,
  RelatedVerseId INTEGER
);
``` 
Bu yapı, bir ayetin birden çok ilgili ayeti olabileceği (örn. konusal bağlantı, benzer tema) durumunu destekler【34†L100-L108】. SQLite için C#’ta Microsoft.Data.Sqlite kütüphanesi kullanılabilir; örnek olarak bağlantı kurma şu şekildedir【14†L66-L74】:  
```csharp
using var conn = new SqliteConnection("Data Source=kuran.db");
conn.Open();
```  
Bu sayede SQL komutları ile yukarıdaki tablolar oluşturulup CRUD işlemleri yapılabilir【14†L66-L74】.  

# C# API Uç Noktaları  
Backend, RESTful C# Web API olarak tasarlanmalıdır. Aşağıdaki uç noktalara örnek verilebilir:  
- `GET /verses` – Veritabanındaki tüm ayetleri listeler. **Sıralama:** Gösterimde *indirme sırasına göre* (download timestamp) olabilir, ancak her kayıt kendi Surah/Ayet numarasını tutar.  
- `GET /verses/{id}` – Belirli bir ayeti getirir (Arapça, Türkçe meal, özet vs.).  
- `POST /verses/{id}/markread` – İlgili ayetin `IsRead` bayrağını *true* olarak işaretler.  
- `GET /verses/{id}/connections` – Ayetin `VerseLinks` tablosundan ilgili diğer ayetlerin ID’lerini döner. Kullanıcı buradan bağlı ayetlere geçiş yapabilir. (İlgili ayetler arası bağlantıların zenginleştirilmesi için tefsir kaynaklarındaki ipuçları ve Munâsebet ilmi referans alınabilir【24†L86-L90】.)  
- `GET /notifications/unreadcount` – Kullanıcıya kaç adet okunmamış ayet kaldığını bildirir. Uygulama ilk açıldığında “Okunmamış mesajınız: **N**” şeklinde bildirim gösterilecektir; burada **N**, toplam ayet sayısına eşittir【32†L47-L50】.  
- `GET /notifications/unread` – Okunmamış ayetlerin kısa listesini dönebilir (örneğin sadece ID’leri).  
Uygulama mimarisinde, her uç nokta ilgili veritabanı işlemlerini yapmalı ve JSON döndürmelidir. Örneğin veri çekerken `SqliteCommand` kullanılarak sorgular oluşturulur ve `reader` ile sonuçlar okunur【14†L66-L74】.  

# Yapay Zeka (LLM) Özeti ve İş Akışı  
Her ayetin ne anlattığı hakkında özet (summary) AI tarafından oluşturulacaktır. Backend’e her ayet verisi alındığında, bu metin üzerinde bir LLM’e (örn. GPT-4o) özet çıkarma promptu çalıştırılabilir. Örneğin prompt içinde ayetin Türkçe meali ile Arapça ifadeleri vererek “Bu ayetin özünü 2-3 cümle ile açıkla” şeklinde bir istek yapılır. Ancak Arapça dini metinlerde model hatalarına dikkat etmek gerekir; **güvenilirlik** için çıktılar mutlaka kaynaklarla veya klasik tefsir ile teyit edilmelidir. Nitekim, literatürde “LLM’ler İslami kaynaklara rehberlik için kullanılırken metinleri yanlış aktarma riski taşır” uyarısı vardır【36†L34-L42】. Bu nedenle summary alanı alınan Cevaplar doğruluk kontrolünden geçirilmeli, gerekirse birkaç model veya tasfirat (Ibn Kathir, Elmalılı vb.) ile karşılaştırılmalıdır【36†L34-L42】. Özetler, `Verses` tablosundaki `Summary` alanına kaydedilir.  

## Unread Bildirimi ve Kullanıcı Arayüzü  
Kullanıcı arayüzü açıldığında sol veya ana sayfada kırmızı nokta ile birlikte “Okunmamış mesajınız: **N**” sayısı gösterilmelidir. Bu tasarım, mobil uygulamalarda unread mesaj sayısını vurgulayan standart bir desendir【32†L47-L50】. Kullanıcı bir ayeti görüntüledikten sonra, arayüzde “Okundu olarak işaretle” butonu sunulabilir; bu, `POST /verses/{id}/markread` çağrısı ile veritabanını günceller ve bildirimi azaltır. Böylece bir sonraki girişte aynı ayet tekrar okunmamış görünmez.  

# Verse Bağlantıları ve İlişkisel Yapı  
Ayetler arasında geçiş yapılabilmesi için, her ayetin ilişkili olduğu diğer ayetleri gösteren bir mekanizma gerekir. Bunlar, “munâsebet” veya “i’caz” bağlamında literatürde işaret edilir【24†L86-L90】. Örneğin, Bediüzzaman Said Nursî’ye göre her ayet, Kur’an’ın diğer ayetlerine “merkez ve kardeş” gibidir【24†L86-L90】. Bu nedenle sistem, VerseLinks tablosunu kullanarak kullanıcıya bağlı ayetleri sunabilir. Örneğin, surah 2, ayet 254’ün benzeri veya ek açıklamalı satırları varsa, bu ilişkili ID’ler VerseLinks’e eklenir. Veritabanında `VerseLinks` tablo yapısı bir join tablosu gibidir (bkz. [34†L100-L108]) ve her bağlantı iki yönde işlenebilir (hem verse1→verse2 hem de verse2→verse1 olarak ekleme yapılabilir). Böylece kullanıcı bir ayetin detay sayfasında bu bağlantıları görerek tıklayabilir.  

# Bellek (Memory) Yönetimi  
Arka planda çalışan AI ajanının, her sohbet ve işlem sırasında hangi ayetlere erişildiği, hangi verilerin kaydedildiği bilgilerini tutması gerekir. Bunun için basit bir **memory** dosyası kullanılabilir. Örneğin her oturum veya gün için bir JSON/markdown dosyası saklanarak (örn. `memory_<kullaniciID>.json`) ayetlerin okunma durumu, özet oluşturma logu gibi bilgiler tutulabilir【17†L83-L91】【17†L139-L148】. Bu yöntem, Medium’da anlatıldığı üzere bir günlük gibi çalışabilir: “her gün için memory/2026-02-07.md” gibi bir dosya açılarak konuşma ve kararlar yazılır【17†L83-L91】. Markdown formatında tutulan bu kayıtlar hem okunabilir hem de değiştirilebilir olmasıyla faydalıdır【17†L139-L148】. Böylece AI ajanı her açılışta bu dosyayı okuyarak kaldığı yerden devam edebilir ve daha önce işaretlenen “okundu” bilgilerini güncel tutabilir.  

# AI Kod Ajanına Verilecek Örnek Prompt  
Aşağıdaki örnek prompt, AI kod ajanın backend kodunu yazarken izlemesi gereken detayları içerir (kaynak linkleri `.md` dosyasında listelenmelidir):  

```
“Kuran ayetlerini yönetecek bir C# Web API geliştir. Kullanılacak veritabanı SQLite olacak. Verseler için `Verses` adlı bir tablo oluştur: Surah numarası, ayet numarası, Arapça metin, Türkçe meal, AI özet (Summary), IsRead (bool), DownloadedAt zamanı alanları bulunsun【26†L105-L113】【14†L66-L74】. Ayetler arası çoklu bağlantı için `VerseLinks` adlı bir ilişki tablosu oluştur (öz-ID, VerseId, RelatedVerseId)【34†L100-L108】. Verseler alınırken öncelikle Diyanet Kuran API veya Quran.Foundation API kullanarak Arapça ve meal verisini çek. Meali ve mücmel tefsir bilgiisi `TurkishTranslation` alanına, Arapça metin `ArabicText`’e kaydet【26†L105-L113】【28†L63-L70】. Her ayet için bir LLM özeti oluştur ve `Summary` alanına yazdır. LLM’e özet çıkarırken kutsal metinlerde hatayı önlemek için GPT-4o gibi yüksek güvenilirlikli modeli kullan ve çıktıların doğruluğunu klasik tefsirlerle karşılaştır【36†L34-L42】【24†L86-L90】.  

API uç noktaları: 
- `GET /verses`: Tüm ayetleri JSON liste olarak döndür. Listede her kayıt `Id`, `SurahId`, `VerseNumber`, `IsRead` bilgisi olsun (ayeti okumak için `GET /verses/{id}` kullanılacak). Listeleme indirilme zamanına göre olabilir【32†L47-L50】. 
- `GET /verses/{id}`: ID’ye göre tek bir ayetin detayını döndür. Dönüşte Arapça, meal, özet, okundu durumu yer alsın. 
- `POST /verses/{id}/markread`: İlgili ayetin `IsRead` bayrağını true yapıp günceller. Bu çağrıdan sonra ayet okunmuş sayılacak. 
- `GET /verses/{id}/connections`: O ayetin `VerseLinks` tablosundaki bağlantılarını döndürür. Kullanıcı, bu endpoint’ten gelen ilgili ayet ID’lerine tıklayarak bağlantılı ayetlere geçebilsin. 
- `GET /notifications/unreadcount`: Toplam okunmamış ayet sayısını (yani henüz `IsRead=false` olan ayet adedini) döndürür. Uygulama açılışında “Okunmamış mesajınız: N” bildirimi bu sayıdan alınmalıdır【32†L47-L50】. 

Her oturum sonunda ve başında ajanın durumunu hatırlaması için bir memory dosyası (örn. JSON/markdown) kullan. Bu dosyada okunan ayetler, son kullanılan ayet ID’si gibi bilgiler tutulmalı【17†L83-L91】. AI ajan kodu, her işlemin sonunda ilgili veriyi bu dosyaya yazmalı ve sohbete yeniden döndüğünde bu dosyayı okuyarak kaldığı yerden devam edebilmelidir. 

Son olarak, kullanılacak veri kaynakları `sources.md` dosyasında listelenecek: Diyanet Kuran API linkleri【26†L105-L113】, Al-Quran Cloud ve Quran.Foundation API dokümanları【28†L63-L70】【30†L229-L236】, güvenilir tefsir referansları ve diğer meal kaynaklarına ait linkler bu dosyada yer alsın.” 
```

Yukarıdaki prompt AI’ye verilerek arka uç kodunun oluşturulması sağlanabilir. Prompt’ta belirtilen teknik detaylar ve referans bağlantıları, ajanın doğru kaynaklardan veri çekmesini ve belirtilen işlevleri gerçekleştirmesini sağlayacaktır.  

**Kaynaklar (.md dosyası)**:  
- Diyanet Kuran Portalı (Arapça metin + Türkçe Meali)【26†L105-L113】  
- Quran.Foundation içerik API’leri (Ayet, meal, tefsir)【28†L63-L70】【30†L229-L236】  
- AlQuran.cloud API dokümanları (Kur’an metni & tercümeler)【7†L63-L71】  
- Kuran Meali/Tefsir siteleri (Elmalılı, Diyanet, vb. meal kaynakları)【29†L220-L228】【25†L186-L194】  
- Ayetler arası bağlantılar ve “Munâsebet” ilmi hakkında akademik kaynaklar (bakınız Bediüzzaman’ın sözleri)【24†L86-L90】.