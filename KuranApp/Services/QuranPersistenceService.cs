using KuranApp.Data;
using KuranApp.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;

namespace KuranApp.Services
{
    public class QuranPersistenceService
    {
        private readonly DatabaseHelper _db;
        private readonly string _dataDirectory;

        private static readonly int[] RevelationOrderOfSurahs = new int[]
        {
            96, 68, 73, 74, 1, 111, 81, 87, 92, 89, 93, 94, 103, 100, 108, 102, 107, 109, 105, 113, 114, 112, 53, 80, 97, 91, 85, 95, 106, 101, 75, 104, 77, 50, 90, 86, 54, 38, 7, 72, 36, 25, 35, 19, 20, 56, 26, 27, 28, 17, 10, 11, 12, 15, 6, 37, 31, 34, 39, 40, 41, 42, 43, 44, 45, 46, 51, 88, 18, 16, 71, 14, 21, 23, 32, 52, 67, 69, 70, 78, 79, 82, 84, 30, 29, 83, 2, 8, 3, 33, 60, 4, 99, 57, 47, 13, 55, 76, 65, 98, 59, 24, 22, 63, 58, 49, 66, 64, 61, 62, 48, 5, 9, 110
        };

        public QuranPersistenceService(DatabaseHelper db)
        {
            _db = db;
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "QuranData");
        }

        public async Task PersistAllInRevelationOrderAsync()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                throw new DirectoryNotFoundException($"Veri dizini bulunamadı: {_dataDirectory}. Lütfen önce sync işlemini çalıştırın.");
            }

            _db.ClearAllData();

            foreach (int surahNumber in RevelationOrderOfSurahs)
            {
                string filePath = Path.Combine(_dataDirectory, $"surah_{surahNumber}.json");
                if (!File.Exists(filePath)) continue;

                await ProcessSurahFileAsync(surahNumber, filePath);
                _db.SetSyncState("LastProcessedSurah", surahNumber.ToString());
                Console.WriteLine($"Sure {surahNumber} başarıyla veritabanına kaydedildi.");
            }
        }

        public async Task<string?> GenerateInterpretationAsync(Verse verse, LLMModelInfo modelInfo, string promptKey = "default")
        {
            if (string.IsNullOrEmpty(modelInfo.ApiKey) && modelInfo.ProviderName.ToLower() != "lmstudio")
            {
                return null;
            }

            try
            {
                return modelInfo.ProviderName.ToLower() switch
                {
                    "groq" => await CallGroqAPI(verse, modelInfo, promptKey),
                    "openrouter" => await CallOpenRouterAPI(verse, modelInfo, promptKey),
                    "together ai" => await CallTogetherAIAPI(verse, modelInfo, promptKey),
                    "google gemini" => await CallGeminiAPI(verse, modelInfo, promptKey),
                    "lmstudio" => await CallLMStudioAPI(verse, modelInfo, promptKey),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LLM API Hatası ({modelInfo.ProviderName}): {ex.Message}");
                return null;
            }
        }

        private async Task<string?> CallGroqAPI(Verse verse, LLMModelInfo modelInfo, string promptKey)
        {
            var prompt = BuildInterpretationPrompt(verse, promptKey);

            var requestBody = new
            {
                model = modelInfo.ModelName,
                messages = new[]
                {
                    new { role = "system", content = "Sen saygın ve bilgili bir İslam alimi, tarihçi ve dil bilimcisin. Kuran ayetlerini derinlemesine, akademik ve manevi bir perspektifle yorumluyorsun." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(3600);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {modelInfo.ApiKey}");

            var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }

            Console.WriteLine($"Groq API Error: {responseString}");
            return null;
        }

        private async Task<string?> CallOpenRouterAPI(Verse verse, LLMModelInfo modelInfo, string promptKey)
        {
            var prompt = BuildInterpretationPrompt(verse, promptKey);

            var requestBody = new
            {
                model = modelInfo.ModelName,
                messages = new[]
                {
                    new { role = "system", content = "Sen saygın ve bilgili bir İslam alimi, tarihçi ve dil bilimcisin. Kuran ayetlerini derinlemesine, akademik ve manevi bir perspektifle yorumluyorsun." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {modelInfo.ApiKey}");

            var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }

            Console.WriteLine($"OpenRouter API Error: {responseString}");
            return null;
        }

        private async Task<string?> CallTogetherAIAPI(Verse verse, LLMModelInfo modelInfo, string promptKey)
        {
            var prompt = BuildInterpretationPrompt(verse, promptKey);

            var requestBody = new
            {
                model = modelInfo.ModelName,
                messages = new[]
                {
                    new { role = "system", content = "Sen saygın ve bilgili bir İslam alimi, tarihçi ve dil bilimcisin. Kuran ayetlerini derinlemesine, akademik ve manevi bir perspektifle yorumluyorsun." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {modelInfo.ApiKey}");

            var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.togetherai.ai/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }

            Console.WriteLine($"Together AI API Error: {responseString}");
            return null;
        }

        private async Task<string?> CallGeminiAPI(Verse verse, LLMModelInfo modelInfo, string promptKey)
        {
            var prompt = BuildInterpretationPrompt(verse, promptKey);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2000
                }
            };

            using var client = new HttpClient();

            var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/{modelInfo.ModelName}:generateContent?key={modelInfo.ApiKey}",
                content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }

            Console.WriteLine($"Gemini API Error: {responseString}");
            return null;
        }

        private async Task<string?> CallLMStudioAPI(Verse verse, LLMModelInfo modelInfo, string promptKey)
        {
            var prompt = BuildInterpretationPrompt(verse, promptKey);
            var baseUrl = modelInfo.BaseUrl ?? "http://127.0.0.1:1234/v1";

            var requestBody = new
            {
                model = modelInfo.ModelName,
                messages = new[]
                {
                    new { role = "system", content = "Sen saygın ve bilgili bir İslam alimi, tarihçi ve dil bilimcisin. Kuran ayetlerini derinlemesine, akademik ve manevi bir perspektifle yorumluyorsun." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            using var client = new HttpClient();
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{baseUrl}/chat/completions", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    return doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                }

                Console.WriteLine($"LMStudio API Error: {responseString}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LMStudio Connection Error: {ex.Message}. Make sure LMStudio is running on {baseUrl}");
                return null;
            }
        }

        private string BuildInterpretationPrompt(Verse verse, string promptKey)
        {
            if (promptKey == "detailed")
            {
                return $@"Aşağıdaki Kuran ayetini detaylı bir şekilde analiz et:
Arapça Metin: ""{verse.ArabicText}""
Türkçe Meal: ""{verse.TurkishTranslation}""
Bu ayeti tarafsız, nesnel, kısa ve net bir şekilde TÜRKÇE olarak yorumla.
Anlam kaybı olmayacak şekilde mümkün olduğunca öz ve veciz bir anlatım kullan.
Lütfen aşağıdaki başlıkları kullanarak, formatlı bir şekilde yanıt ver:

### 1. Meal-Merkezli Kısa Açıklama
Ayette tam olarak ne dendiğini, kelime anlamlarını da gözeterek 1-2 cümle ile açıkla.

### 2. Yorum/Tefsir
Ayetin derin anlamını, mesajını ve günümüze bakan yönlerini detaylıca açıkla.

### 3. Hadis
Bu ayetle ilgili veya bu ayetin mesajını destekleyen bilinen sahih hadislerden bahset.

### 4. Tarih
Ayetin indiği dönemin tarihi arka planı, o dönemdeki olaylar ve toplumsal yapı hakkında bilgi ver.

### 5. Sebeb-i Nüzul
Ayetin iniş sebebini (eğer biliniyorsa özel bir olay veya soru üzerine mi indi) açıkla.

Yanıtın sadece bu başlıkları ve içeriklerini içermeli ve eğer başlık için bilgi kesin değilse 'bilinmemektedir' yaz, TÜRKÇE olmalı ve akademik/saygın bir dil kullanılmalıdır.";
            }

            // Default prompt (original)
            return $"Şu Kuran ayetinin Arapça metni: \"{verse.ArabicText}\" Türkçe meali: \"{verse.TurkishTranslation}\" " +
                   $"Bu ayeti tarafsız, nesnel, kısa ve net bir şekilde TÜRKÇE olarak yorumla. " +
                   $"Anlam kaybı olmayacak şekilde mümkün olduğunca öz ve veciz bir anlatım kullan. " +
                   $"Yanıtın sadece Türkçe ve yorum metni olmalıdır.";
        }

        public void SaveInterpretationToFile(int surahNumber, int modelId, string content, string promptKey = "default")
        {
            _db.SaveInterpretationToFile(surahNumber, modelId, content, promptKey);
        }

        private async Task ProcessSurahFileAsync(int surahNumber, string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var doc = JsonDocument.Parse(json);

            string surahName = doc.RootElement.GetProperty("SurahName").GetString() ?? "";
            string surahEnglishName = doc.RootElement.GetProperty("SurahEnglishName").GetString() ?? "";
            string surahMeaning = doc.RootElement.TryGetProperty("SurahMeaning", out var meaningProp) ? meaningProp.GetString() ?? "" : "";
            int surahRevelationOrder = doc.RootElement.GetProperty("RevelationOrder").GetInt32();
            var verses = doc.RootElement.GetProperty("Verses");
            int verseCount = verses.GetArrayLength();

            var surah = new Surah
            {
                SurahNumber = surahNumber,
                Name = surahName,
                EnglishName = surahEnglishName,
                Meaning = surahMeaning,
                RevelationOrder = surahRevelationOrder,
                VerseCount = verseCount
            };
            _db.AddSurah(surah);

            foreach (var ayah in verses.EnumerateArray())
            {
                int ayahNumber = ayah.TryGetProperty("Number", out var numProp) ? numProp.GetInt32() : 0;
                int revelationOrder = ayah.TryGetProperty("RevelationOrder", out var revProp) ? revProp.GetInt32() : 0;
                string arabicText = ayah.TryGetProperty("ArabicText", out var arProp) ? arProp.GetString() ?? "" : "";
                string turkishTranslation = ayah.TryGetProperty("TurkishTranslation", out var trProp) ? trProp.GetString() ?? "" : "";

                var verse = new Verse
                {
                    SurahId = surahNumber,
                    VerseNumber = ayahNumber,
                    RevelationOrder = revelationOrder,
                    ArabicText = arabicText,
                    TurkishTranslation = turkishTranslation,
                    DownloadedAt = DateTime.Now
                };

                _db.AddVerse(verse);
            }
        }
    }
}