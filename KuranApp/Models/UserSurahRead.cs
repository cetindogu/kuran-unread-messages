using System;

namespace KuranApp.Models
{
    public class UserSurahRead
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SurahId { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
