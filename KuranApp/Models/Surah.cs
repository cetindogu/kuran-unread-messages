using System;

namespace KuranApp.Models
{
    public class Surah
    {
        public int Id { get; set; }
        public int SurahNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public int RevelationOrder { get; set; }
        public int VerseCount { get; set; }
    }
}
