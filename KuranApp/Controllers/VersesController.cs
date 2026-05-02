using Microsoft.AspNetCore.Mvc;
using KuranApp.Data;
using KuranApp.Models;
using KuranApp.Services;
using System.Collections.Generic;

namespace KuranApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersesController : ControllerBase
    {
        private readonly DatabaseHelper _db;
        private readonly IAIService _aiService;
        private readonly QuranPersistenceService _persistenceService;

        public VersesController(DatabaseHelper db, IAIService aiService, QuranPersistenceService persistenceService)
        {
            _db = db;
            _aiService = aiService;
            _persistenceService = persistenceService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync()
        {
            // Veritabanını temizleyelim
            _db.ClearAllData();

            var downloader = new QuranDataDownloader();
            await downloader.DownloadAllQuranDataAsync();
            
            await _persistenceService.PersistAllInRevelationOrderAsync();
            
            return Ok("Sync and Persistence completed.");
        }

        [HttpGet("{id}/interpretations")]
        public IEnumerable<AIInterpretation> GetInterpretations(int id)
        {
            return _db.GetAIInterpretations(id);
        }

        [HttpGet]
        public IEnumerable<object> Get(int userId = 1)
        {
            var verses = _db.GetAllVerses();
            var result = new List<object>();
            foreach (var v in verses)
            {
                result.Add(new
                {
                    v.Id,
                    v.SurahId,
                    v.VerseNumber,
                    v.RevelationOrder,
                    v.ArabicText,
                    v.TurkishTranslation,
                    v.Summary,
                    v.DownloadedAt,
                    IsRead = _db.IsVerseRead(userId, v.Id)
                });
            }
            return result;
        }

        [HttpGet("{id}")]
        public ActionResult<object> Get(int id, int userId = 1)
        {
            var v = _db.GetVerseById(id);
            if (v == null) return NotFound();
            
            return new
            {
                v.Id,
                v.SurahId,
                v.VerseNumber,
                v.RevelationOrder,
                v.ArabicText,
                v.TurkishTranslation,
                v.Summary,
                v.DownloadedAt,
                IsRead = _db.IsVerseRead(userId, v.Id)
            };
        }

        [HttpPost("{id}/markread")]
        public IActionResult MarkRead(int id, int userId = 1)
        {
            _db.MarkAsRead(userId, id);
            return Ok();
        }

        [HttpGet("{id}/connections")]
        public IEnumerable<int> GetConnections(int id)
        {
            return _db.GetVerseConnections(id);
        }

        [HttpGet("/notifications/unreadcount")]
        public ActionResult<int> GetUnreadCount(int userId = 1)
        {
            return _db.GetUnreadCount(userId);
        }
    }
}
