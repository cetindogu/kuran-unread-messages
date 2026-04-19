using System.Net.Http.Json;
using System.Text.Json;
using KuranApp.Models;

namespace KuranApp.Services
{
    public class QuranDataDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly string _dataDirectory;

        public QuranDataDownloader()
        {
            _httpClient = new HttpClient();
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "QuranData");
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        // Nüzul Sırası
        private static readonly int[] RevelationOrderOfSurahs = new int[] 
        { 
            96, 68, 73, 74, 1, 111, 81, 87, 92, 89, 93, 94, 103, 100, 108, 102, 107, 109, 105, 113, 114, 112, 53, 80, 97, 91, 85, 95, 106, 101, 75, 104, 77, 50, 90, 86, 54, 38, 7, 72, 36, 25, 35, 19, 20, 56, 26, 27, 28, 17, 10, 11, 12, 15, 6, 37, 31, 34, 39, 40, 41, 42, 43, 44, 45, 46, 51, 88, 18, 16, 71, 14, 21, 23, 32, 52, 67, 69, 70, 78, 79, 82, 84, 30, 29, 83, 2, 8, 3, 33, 60, 4, 99, 57, 47, 13, 55, 76, 65, 98, 59, 24, 22, 63, 58, 49, 66, 64, 61, 62, 48, 5, 9, 110
        };

        public async Task DownloadAllQuranDataAsync()
        {
            Console.WriteLine("İndirme işlemi başlıyor...");

            try
            {
                var arabicResponse = await _httpClient.GetFromJsonAsync<QuranApiResponse>("http://api.alquran.cloud/v1/quran/quran-uthmani");
                var turkishResponse = await _httpClient.GetFromJsonAsync<QuranApiResponse>("http://api.alquran.cloud/v1/quran/tr.diyanet");

                if (arabicResponse?.Data?.Surahs == null || turkishResponse?.Data?.Surahs == null)
                {
                    Console.WriteLine("API'den veri alınamadı.");
                    return;
                }

                int globalRevelationIndex = 1;

                // Nüzul sırasına göre global indeksi hesaplamak için önce tüm veriyi bir haritada tutalım
                var arabicSurahs = arabicResponse.Data.Surahs.ToDictionary(s => s.Number);
                var turkishSurahs = turkishResponse.Data.Surahs.ToDictionary(s => s.Number);

                foreach (int surahNumber in RevelationOrderOfSurahs)
                {
                    var surahArabic = arabicSurahs[surahNumber];
                    var surahTurkish = turkishSurahs[surahNumber];

                    var verses = new List<object>();
                    for (int i = 0; i < surahArabic.Ayahs.Count; i++)
                    {
                        verses.Add(new
                        {
                            Number = surahArabic.Ayahs[i].NumberInSurah,
                            RevelationOrder = globalRevelationIndex++,
                            ArabicText = surahArabic.Ayahs[i].Text,
                            TurkishTranslation = surahTurkish.Ayahs[i].Text
                        });
                    }

                    var surahData = new
                    {
                        SurahNumber = surahNumber,
                        SurahName = surahArabic.Name,
                        SurahEnglishName = surahArabic.EnglishName,
                        RevelationOrder = Array.IndexOf(RevelationOrderOfSurahs, surahNumber) + 1,
                        Verses = verses
                    };

                    string fileName = Path.Combine(_dataDirectory, $"surah_{surahNumber}.json");
                    await File.WriteAllTextAsync(fileName, JsonSerializer.Serialize(surahData, new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"Sure {surahNumber} ({surahArabic.EnglishName}) indirildi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
            }
        }
    }

    public class QuranApiResponse
    {
        public int Code { get; set; }
        public string Status { get; set; } = string.Empty;
        public QuranData Data { get; set; } = new();
    }

    public class QuranData
    {
        public List<SurahResponse> Surahs { get; set; } = new();
    }

    public class SurahResponse
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public List<AyahResponse> Ayahs { get; set; } = new();
    }

    public class AyahResponse
    {
        public int NumberInSurah { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
