using Microsoft.AspNetCore.Mvc;
using KuranApp.Data;
using KuranApp.Models;
using KuranApp.Services;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace KuranApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersesController : ControllerBase
    {
        private readonly DatabaseHelper _db;
        private readonly QuranPersistenceService _persistenceService;

        public VersesController(DatabaseHelper db, QuranPersistenceService persistenceService)
        {
            _db = db;
            _persistenceService = persistenceService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync()
        {
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

        [HttpGet("{id}/interpretations/{modelId}")]
        public ActionResult<AIInterpretation> GetInterpretation(int id, int modelId)
        {
            var interpretation = _db.GetAIInterpretation(id, modelId);
            if (interpretation == null)
                return NotFound();
            return interpretation;
        }

        [HttpPost("{id}/interpretations/{modelId}/generate")]
        public async Task<ActionResult<AIInterpretation>> GenerateInterpretation(int id, int modelId)
        {
            var verse = _db.GetVerseById(id);
            if (verse == null)
                return NotFound("Verse not found.");

            var modelInfo = _db.GetLLMModel(modelId);
            if (modelInfo == null)
                return NotFound("Model not found.");

            var existing = _db.GetAIInterpretation(id, modelId);
            if (existing != null)
                return existing;

            var result = await _persistenceService.GenerateInterpretationAsync(verse, modelInfo);
            if (result == null)
                return BadRequest("Yorum üretilemedi (API hatası veya model yüklü değil).");

            var interpretation = new AIInterpretation
            {
                VerseId = id,
                ModelName = modelInfo.DisplayName,
                Interpretation = result,
                GeneratedAt = DateTime.Now
            };

            _db.AddAIInterpretation(id, modelId, result);
            _persistenceService.SaveInterpretationToFile(verse.SurahId, modelId, result);

            return interpretation;
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