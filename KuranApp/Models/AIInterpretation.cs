using System;

namespace KuranApp.Models
{
    public class AIInterpretation
    {
        public int Id { get; set; }
        public int VerseId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
