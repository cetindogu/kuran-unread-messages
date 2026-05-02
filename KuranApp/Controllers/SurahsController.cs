using Microsoft.AspNetCore.Mvc;
using KuranApp.Data;
using KuranApp.Models;
using System.Collections.Generic;

namespace KuranApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SurahsController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public SurahsController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IEnumerable<object> Get(int userId = 1)
        {
            var surahs = _db.GetAllSurahs(userId);
            var result = new List<object>();
            foreach (var s in surahs)
            {
                result.Add(new
                {
                    s.Id,
                    s.SurahNumber,
                    s.Name,
                    s.EnglishName,
                    s.Meaning,
                    s.RevelationOrder,
                    s.VerseCount,
                    IsRead = _db.IsSurahRead(userId, s.Id)
                });
            }
            return result;
        }

        [HttpPost("{id}/markread")]
        public IActionResult MarkRead(int id, int userId = 1)
        {
            _db.MarkSurahAsRead(userId, id);
            return Ok();
        }

        [HttpGet("{surahNumber}/verses")]
        public IEnumerable<Verse> GetVerses(int surahNumber)
        {
            return _db.GetVersesBySurah(surahNumber);
        }

        [HttpPost("reset")]
        public IActionResult Reset()
        {
            _db.ClearAllData();
            return Ok("Database cleared. Please run sync again.");
        }
    }
}
