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

        public async Task DownloadAllQuranDataAsync()
        {
            // AlQuran.cloud API endpoints
            // Arabic: http://api.alquran.cloud/v1/quran/quran-uthmani
            // Turkish: http://api.alquran.cloud/v1/quran/tr.diyanet

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

                for (int i = 0; i < arabicResponse.Data.Surahs.Count; i++)
                {
                    var surahArabic = arabicResponse.Data.Surahs[i];
                    var surahTurkish = turkishResponse.Data.Surahs[i];

                    var surahData = new
                    {
                        SurahNumber = surahArabic.Number,
                        SurahName = surahArabic.Name,
                        SurahEnglishName = surahArabic.EnglishName,
                        Verses = surahArabic.Ayahs.Select((a, idx) => new
                        {
                            Number = a.NumberInSurah,
                            ArabicText = a.Text,
                            TurkishTranslation = surahTurkish.Ayahs[idx].Text
                        }).ToList()
                    };

                    string fileName = Path.Combine(_dataDirectory, $"surah_{surahArabic.Number}.json");
                    await File.WriteAllTextAsync(fileName, JsonSerializer.Serialize(surahData, new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"Sure {surahArabic.Number} ({surahArabic.EnglishName}) indirildi.");
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
