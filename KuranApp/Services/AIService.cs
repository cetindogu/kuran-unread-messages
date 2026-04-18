namespace KuranApp.Services
{
    public interface IAIService
    {
        Task<string> GenerateSummaryAsync(string arabicText, string translation, string modelName = "GPT-4o");
    }

    public class AIService : IAIService
    {
        public Task<string> GenerateSummaryAsync(string arabicText, string translation, string modelName = "GPT-4o")
        {
            // Arapça ve Türkçe metin üzerinden özet üretimi (Simülasyon)
            string summary = $"{modelName} Analizi: '{arabicText.Substring(0, Math.Min(10, arabicText.Length))}...' metni ve '{translation.Substring(0, Math.Min(15, translation.Length))}...' meali incelendiğinde; bu ayet Allah'ın yüceliğini ve rehberliğini anlatmaktadır.";
            return Task.FromResult(summary);
        }
    }
}
