using System;

namespace KuranApp.Models
{
    public class Verse
    {
        public int Id { get; set; }
        public int SurahId { get; set; }
        public int VerseNumber { get; set; }
        public int RevelationOrder { get; set; }
        public string ArabicText { get; set; } = string.Empty;
        public string TurkishTranslation { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime DownloadedAt { get; set; }
    }
}
