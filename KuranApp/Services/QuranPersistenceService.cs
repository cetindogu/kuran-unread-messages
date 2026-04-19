using KuranApp.Data;
using KuranApp.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KuranApp.Services
{
    public class QuranPersistenceService
    {
        private readonly DatabaseHelper _db;
        private readonly IAIService _aiService;
        private readonly string _dataDirectory;

        private static readonly int[] RevelationOrderOfSurahs = new int[] 
        { 
            96, 68, 73, 74, 1, 111, 81, 87, 92, 89, 93, 94, 103, 100, 108, 102, 107, 109, 105, 113, 114, 112, 53, 80, 97, 91, 85, 95, 106, 101, 75, 104, 77, 50, 90, 86, 54, 38, 7, 72, 36, 25, 35, 19, 20, 56, 26, 27, 28, 17, 10, 11, 12, 15, 6, 37, 31, 34, 39, 40, 41, 42, 43, 44, 45, 46, 51, 88, 18, 16, 71, 14, 21, 23, 32, 52, 67, 69, 70, 78, 79, 82, 84, 30, 29, 83, 2, 8, 3, 33, 60, 4, 99, 57, 47, 13, 55, 76, 65, 98, 59, 24, 22, 63, 58, 49, 66, 64, 61, 62, 48, 5, 9, 110
        };

        public QuranPersistenceService(DatabaseHelper db, IAIService aiService)
        {
            _db = db;
            _aiService = aiService;
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "QuranData");
        }

        public async Task PersistAllInRevelationOrderAsync()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                throw new DirectoryNotFoundException($"Veri dizini bulunamadı: {_dataDirectory}. Lütfen önce sync işlemini çalıştırın.");
            }

            string? lastProcessedSurahStr = _db.GetSyncState("LastProcessedSurah");
            int lastProcessedSurah = string.IsNullOrEmpty(lastProcessedSurahStr) ? -1 : int.Parse(lastProcessedSurahStr);

            foreach (int surahNumber in RevelationOrderOfSurahs)
            {
                if (IsSurahAlreadyProcessed(surahNumber, lastProcessedSurah)) continue;

                string filePath = Path.Combine(_dataDirectory, $"surah_{surahNumber}.json");
                if (!File.Exists(filePath)) continue;

                await ProcessSurahFileAsync(surahNumber, filePath);
                
                _db.SetSyncState("LastProcessedSurah", surahNumber.ToString());
                Console.WriteLine($"Sure {surahNumber} başarıyla veritabanına kaydedildi.");
            }
        }

        private bool IsSurahAlreadyProcessed(int surahNumber, int lastProcessedSurah)
        {
            if (lastProcessedSurah == -1) return false;
            int currentIndex = Array.IndexOf(RevelationOrderOfSurahs, surahNumber);
            int lastIndex = Array.IndexOf(RevelationOrderOfSurahs, lastProcessedSurah);
            return currentIndex <= lastIndex;
        }

        private async Task ProcessSurahFileAsync(int surahNumber, string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var doc = JsonDocument.Parse(json);
            
            // Log available properties for debugging
            Console.WriteLine($"Processing Surah {surahNumber} from {filePath}");
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                Console.WriteLine($"Found property: {prop.Name}");
            }

            var verses = doc.RootElement.GetProperty("Verses");

            foreach (var ayah in verses.EnumerateArray())
            {
                Console.WriteLine($"Ayah Raw: {ayah.GetRawText()}");
                int ayahNumber = ayah.TryGetProperty("Number", out var numProp) ? numProp.GetInt32() : 0;
                int revelationOrder = ayah.TryGetProperty("RevelationOrder", out var revProp) ? revProp.GetInt32() : 0;
                Console.WriteLine($"Parsed RevOrder: {revelationOrder}");
                string arabicText = ayah.TryGetProperty("ArabicText", out var arProp) ? arProp.GetString() ?? "" : "";
                string turkishTranslation = ayah.TryGetProperty("TurkishTranslation", out var trProp) ? trProp.GetString() ?? "" : "";

                string summary = await _aiService.GenerateSummaryAsync(arabicText, turkishTranslation, "GPT-4o");

                var verse = new Verse
                {
                    SurahId = surahNumber,
                    VerseNumber = ayahNumber,
                    RevelationOrder = revelationOrder,
                    ArabicText = arabicText,
                    TurkishTranslation = turkishTranslation,
                    Summary = summary,
                    DownloadedAt = DateTime.Now
                };

                _db.AddVerse(verse);
                await SaveAIInterpretationsAsync(verse, summary, turkishTranslation);
            }
        }

        private async Task SaveAIInterpretationsAsync(Verse verse, string summary, string translation)
        {
            _db.AddAIInterpretation(new AIInterpretation
            {
                VerseId = verse.Id,
                ModelName = "GPT-4o",
                Interpretation = summary,
                GeneratedAt = DateTime.Now
            });

            _db.AddAIInterpretation(new AIInterpretation
            {
                VerseId = verse.Id,
                ModelName = "Claude-3.5-Sonnet",
                Interpretation = $"[Claude-3.5] {translation} ayeti üzerine hikmetli yorum.",
                GeneratedAt = DateTime.Now
            });
        }

        private class SurahDataFile
        {
            public List<AyahDataFile> Verses { get; set; } = new();
        }

        private class AyahDataFile
        {
            [JsonPropertyName("Number")]
            public int Number { get; set; }
            
            [JsonPropertyName("RevelationOrder")]
            public int RevelationOrder { get; set; }
            
            [JsonPropertyName("ArabicText")]
            public string ArabicText { get; set; } = string.Empty;
            
            [JsonPropertyName("TurkishTranslation")]
            public string TurkishTranslation { get; set; } = string.Empty;
        }
    }
}
