using Microsoft.AspNetCore.Mvc;
using KuranApp.Data;
using KuranApp.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuranApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LLMController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public LLMController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet("models")]
        public ActionResult<IEnumerable<object>> GetModels()
        {
            var models = _db.GetActiveLLMModels();
            var result = new List<object>();
            foreach (var m in models)
            {
                result.Add(new
                {
                    m.Id,
                    m.ModelName,
                    m.DisplayName,
                    m.ProviderName,
                    m.IsFree,
                    HasApiKey = !string.IsNullOrEmpty(m.ApiKey)
                });
            }
            return result;
        }

        [HttpPost("providers")]
        public IActionResult AddProvider([FromBody] AddProviderRequest request)
        {
            if (string.IsNullOrEmpty(request.ProviderName))
                return BadRequest("Provider name is required.");

            var providerId = _db.AddLLMProvider(request.ProviderName, request.ApiKey, request.BaseUrl);
            return Ok(new { ProviderId = providerId });
        }

        [HttpPost("models")]
        public IActionResult AddModel([FromBody] AddModelRequest request)
        {
            if (request.ProviderId <= 0 || string.IsNullOrEmpty(request.ModelName))
                return BadRequest("ProviderId and ModelName are required.");

            var modelId = _db.AddLLMModel(request.ProviderId, request.ModelName, request.DisplayName ?? request.ModelName, request.IsFree);
            return Ok(new { ModelId = modelId });
        }

        [HttpPost("setup-free-models")]
        public IActionResult SetupFreeModels()
        {
            var groqProviderId = _db.AddLLMProvider("Groq", null);
            _db.AddLLMModel(groqProviderId, "llama-3.3-70b-versatile", "Llama 3.3 70B", true);
            _db.AddLLMModel(groqProviderId, "mixtral-8x7b-32768", "Mixtral 8x7B", true);

            var openRouterProviderId = _db.AddLLMProvider("OpenRouter", null);
            _db.AddLLMModel(openRouterProviderId, "google/gemini-2.0-flash", "Gemini 2.0 Flash", true);
            _db.AddLLMModel(openRouterProviderId, "anthropic/claude-3-haiku", "Claude 3 Haiku", true);
            _db.AddLLMModel(openRouterProviderId, "meta-llama/llama-3-8b-instruct", "Llama 3 8B", true);

            var togetherProviderId = _db.AddLLMProvider("Together AI", null);
            _db.AddLLMModel(togetherProviderId, "meta-llama/Llama-3.3-70B-Instruct-Turbo", "Llama 3.3 70B Turbo", true);

            var cloudflareProviderId = _db.AddLLMProvider("Cloudflare Workers AI", null);
            _db.AddLLMModel(cloudflareProviderId, "@cf/meta/llama-3.3-70b-instruct-f16", "Llama 3.3 70B F16", true);

            var lmStudioProviderId = _db.AddLLMProvider("LMStudio", null, "http://127.0.0.1:1234/v1");
            _db.AddLLMModel(lmStudioProviderId, "google/gemma-4-e2b", "Gemma 4 e2b (Local)", true);

            return Ok("Free models configured successfully.");
        }

        [HttpPut("providers/{id}/apiKey")]
        public IActionResult UpdateApiKey(int id, [FromBody] UpdateApiKeyRequest request)
        {
            if (string.IsNullOrEmpty(request.ApiKey))
                return BadRequest("ApiKey is required.");

            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE LLMProviders SET ApiKey = @apiKey WHERE Id = @id";
            command.Parameters.AddWithValue("@apiKey", request.ApiKey);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();

            return Ok();
        }

        [HttpPost("clear-interpretations")]
        public IActionResult ClearInterpretations()
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.ConnectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM AIInterpretations; DELETE FROM FileBackups;";
            command.ExecuteNonQuery();
            return Ok("Interpretations cleared.");
        }

        [HttpPost("batch-interpret")]
        public async Task<IActionResult> BatchInterpret([FromServices] QuranPersistenceService persistenceService, [FromQuery] int modelId, [FromQuery] int? surahId)
        {
            var modelInfo = _db.GetLLMModel(modelId);
            if (modelInfo == null) return NotFound("Model not found.");

            var verses = surahId.HasValue 
                ? _db.GetVersesBySurah(surahId.Value) 
                : _db.GetAllVerses();

            int count = 0;
            foreach (var verse in verses)
            {
                // Check if already interpreted by this model
                var existing = _db.GetAIInterpretation(verse.Id, modelId);
                if (existing != null) continue;

                var result = await persistenceService.GenerateInterpretationAsync(verse, modelInfo);
                if (result == null) continue;

                _db.AddAIInterpretation(verse.Id, modelId, result);
                
                // Save to file as requested
                persistenceService.SaveInterpretationToFile(verse.SurahId, modelId, result);
                count++;
            }

            return Ok(new { Message = $"Processed {count} verses.", Model = modelInfo.DisplayName });
        }
    }

    public class AddProviderRequest
    {
        public string ProviderName { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public string? BaseUrl { get; set; }
    }

    public class AddModelRequest
    {
        public int ProviderId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsFree { get; set; } = true;
    }

    public class UpdateApiKeyRequest
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}