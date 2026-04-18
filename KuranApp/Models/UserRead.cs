using System;

namespace KuranApp.Models
{
    public class UserRead
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int VerseId { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
