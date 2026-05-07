using Microsoft.Data.Sqlite;
using KuranApp.Models;
using System.Collections.Generic;
using System;

namespace KuranApp.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;
        public string ConnectionString => _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=kuran.db";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                cmd.ExecuteNonQuery();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS Surahs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SurahNumber INTEGER UNIQUE,
                    Name TEXT,
                    EnglishName TEXT,
                    Meaning TEXT,
                    RevelationOrder INTEGER,
                    VerseCount INTEGER,
                    UNIQUE(SurahNumber)
                );
                CREATE TABLE IF NOT EXISTS Verses (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SurahId INTEGER,
                    VerseNumber INTEGER,
                    RevelationOrder INTEGER,
                    ArabicText TEXT,
                    TurkishTranslation TEXT,
                    Summary TEXT,
                    DownloadedAt DATETIME,
                    UNIQUE(SurahId, VerseNumber)
                );
                CREATE TABLE IF NOT EXISTS UserReads (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    VerseId INTEGER,
                    ReadAt DATETIME,
                    FOREIGN KEY(UserId) REFERENCES Users(Id),
                    FOREIGN KEY(VerseId) REFERENCES Verses(Id),
                    UNIQUE(UserId, VerseId)
                );
                CREATE TABLE IF NOT EXISTS VerseLinks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VerseId INTEGER,
                    RelatedVerseId INTEGER,
                    FOREIGN KEY(VerseId) REFERENCES Verses(Id),
                    FOREIGN KEY(RelatedVerseId) REFERENCES Verses(Id)
                );
                CREATE TABLE IF NOT EXISTS LLMProviders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProviderName TEXT UNIQUE,
                    ApiKey TEXT,
                    BaseUrl TEXT,
                    IsActive INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS LLMModels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProviderId INTEGER,
                    ModelName TEXT,
                    DisplayName TEXT,
                    IsFree INTEGER DEFAULT 1,
                    IsActive INTEGER DEFAULT 1,
                    FOREIGN KEY(ProviderId) REFERENCES LLMProviders(Id),
                    UNIQUE(ProviderId, ModelName)
                );
                CREATE TABLE IF NOT EXISTS AIInterpretations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VerseId INTEGER,
                    ModelId INTEGER,
                    Interpretation TEXT,
                    GeneratedAt DATETIME,
                    CostTokens INTEGER DEFAULT 0,
                    FOREIGN KEY(VerseId) REFERENCES Verses(Id),
                    FOREIGN KEY(ModelId) REFERENCES LLMModels(Id),
                    UNIQUE(VerseId, ModelId)
                );
                CREATE TABLE IF NOT EXISTS InterpretationRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VerseId INTEGER,
                    ModelId INTEGER,
                    Status TEXT DEFAULT 'pending',
                    RequestedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CompletedAt DATETIME,
                    ErrorMessage TEXT,
                    FOREIGN KEY(VerseId) REFERENCES Verses(Id),
                    FOREIGN KEY(ModelId) REFERENCES LLMModels(Id)
                );
                CREATE TABLE IF NOT EXISTS SyncState (
                    Key TEXT PRIMARY KEY,
                    Value TEXT,
                    UpdatedAt DATETIME
                );
                CREATE TABLE IF NOT EXISTS UserSurahReads (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    SurahId INTEGER,
                    ReadAt DATETIME,
                    FOREIGN KEY(UserId) REFERENCES Users(Id),
                    FOREIGN KEY(SurahId) REFERENCES Surahs(Id),
                    UNIQUE(UserId, SurahId)
                );
                CREATE TABLE IF NOT EXISTS FileBackups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SurahNumber INTEGER,
                    ModelId INTEGER,
                    FilePath TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(SurahNumber) REFERENCES Surahs(SurahNumber),
                    FOREIGN KEY(ModelId) REFERENCES LLMModels(Id),
                    UNIQUE(SurahNumber, ModelId)
                );";
            command.ExecuteNonQuery();

            // Migration: Add Summary column to Verses if it doesn't exist
            try
            {
                var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE Verses ADD COLUMN Summary TEXT;";
                alterCmd.ExecuteNonQuery();
            }
            catch { /* Column already exists */ }

            // Migration: Add ModelId column to AIInterpretations if it doesn't exist
            try
            {
                var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE AIInterpretations ADD COLUMN ModelId INTEGER;";
                alterCmd.ExecuteNonQuery();
            }
            catch { /* Column already exists */ }

            // Migration: Add CostTokens column to AIInterpretations if it doesn't exist
            try
            {
                var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE AIInterpretations ADD COLUMN CostTokens INTEGER DEFAULT 0;";
                alterCmd.ExecuteNonQuery();
            }
            catch { /* Column already exists */ }
        }

        public List<Verse> GetAllVerses()
        {
            var verses = new List<Verse>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Verses ORDER BY RevelationOrder";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                verses.Add(ReadVerse(reader));
            }
            return verses;
        }

        public Verse? GetVerseById(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Verses WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadVerse(reader);
            }
            return null;
        }

        private Verse ReadVerse(SqliteDataReader reader)
        {
            return new Verse
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                SurahId = reader.GetInt32(reader.GetOrdinal("SurahId")),
                VerseNumber = reader.GetInt32(reader.GetOrdinal("VerseNumber")),
                RevelationOrder = reader.GetInt32(reader.GetOrdinal("RevelationOrder")),
                ArabicText = reader.IsDBNull(reader.GetOrdinal("ArabicText")) ? "" : reader.GetString(reader.GetOrdinal("ArabicText")),
                TurkishTranslation = reader.IsDBNull(reader.GetOrdinal("TurkishTranslation")) ? "" : reader.GetString(reader.GetOrdinal("TurkishTranslation")),
                Summary = reader.IsDBNull(reader.GetOrdinal("Summary")) ? "" : reader.GetString(reader.GetOrdinal("Summary")),
                DownloadedAt = reader.IsDBNull(reader.GetOrdinal("DownloadedAt")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("DownloadedAt"))
            };
        }

        public void MarkAsRead(int userId, int verseId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO UserReads (UserId, VerseId, ReadAt) VALUES (@userId, @verseId, @readAt)";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@verseId", verseId);
            command.Parameters.AddWithValue("@readAt", DateTime.Now);
            command.ExecuteNonQuery();
        }

        public int GetUnreadCount(int userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM Verses
                WHERE Id NOT IN (SELECT VerseId FROM UserReads WHERE UserId = @userId)";
            command.Parameters.AddWithValue("@userId", userId);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public bool IsVerseRead(int userId, int verseId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM UserReads WHERE UserId = @userId AND VerseId = @verseId";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@verseId", verseId);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        public List<int> GetVerseConnections(int id)
        {
            var connections = new List<int>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT RelatedVerseId FROM VerseLinks WHERE VerseId = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                connections.Add(reader.GetInt32(0));
            }
            return connections;
        }

        public void AddVerse(Verse verse)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Verses (SurahId, VerseNumber, RevelationOrder, ArabicText, TurkishTranslation, Summary, DownloadedAt)
                VALUES (@surahId, @verseNumber, @revelationOrder, @arabicText, @turkishTranslation, @summary, @downloadedAt);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@surahId", verse.SurahId);
            command.Parameters.AddWithValue("@verseNumber", verse.VerseNumber);
            command.Parameters.AddWithValue("@revelationOrder", verse.RevelationOrder);
            command.Parameters.AddWithValue("@arabicText", verse.ArabicText);
            command.Parameters.AddWithValue("@turkishTranslation", verse.TurkishTranslation);
            command.Parameters.AddWithValue("@summary", verse.Summary ?? "");
            command.Parameters.AddWithValue("@downloadedAt", verse.DownloadedAt);

            verse.Id = Convert.ToInt32(command.ExecuteScalar());
        }

        public void AddSurah(Surah surah)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Surahs (SurahNumber, Name, EnglishName, Meaning, RevelationOrder, VerseCount)
                VALUES (@surahNumber, @name, @englishName, @meaning, @revelationOrder, @verseCount);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@surahNumber", surah.SurahNumber);
            command.Parameters.AddWithValue("@name", surah.Name);
            command.Parameters.AddWithValue("@englishName", surah.EnglishName);
            command.Parameters.AddWithValue("@meaning", surah.Meaning);
            command.Parameters.AddWithValue("@revelationOrder", surah.RevelationOrder);
            command.Parameters.AddWithValue("@verseCount", surah.VerseCount);

            surah.Id = Convert.ToInt32(command.ExecuteScalar());
        }

        public List<Surah> GetAllSurahs(int userId = 1)
        {
            var surahs = new List<Surah>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT s.*,
                (SELECT COUNT(*) FROM UserSurahReads WHERE UserId = @userId AND SurahId = s.Id) as IsRead
                FROM Surahs s
                ORDER BY s.RevelationOrder";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                surahs.Add(new Surah
                {
                    Id = reader.GetInt32(0),
                    SurahNumber = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    EnglishName = reader.GetString(3),
                    Meaning = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    RevelationOrder = reader.GetInt32(5),
                    VerseCount = reader.GetInt32(6)
                });
            }
            return surahs;
        }

        public void MarkSurahAsRead(int userId, int surahId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO UserSurahReads (UserId, SurahId, ReadAt) VALUES (@userId, @surahId, @readAt)";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@surahId", surahId);
            command.Parameters.AddWithValue("@readAt", DateTime.Now);
            command.ExecuteNonQuery();
        }

        public bool IsSurahRead(int userId, int surahId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM UserSurahReads WHERE UserId = @userId AND SurahId = @surahId";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@surahId", surahId);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        public int AddUser(string username)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Username) VALUES (@username); SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@username", username);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int GetUserId(string username)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM Users WHERE Username = @username";
            command.Parameters.AddWithValue("@username", username);
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        public void ClearAllData()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;

                cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                cmd.ExecuteNonQuery();

                string[] tables = {
                    "UserSurahReads",
                    "UserReads",
                    "FileBackups",
                    "InterpretationRequests",
                    "AIInterpretations",
                    "VerseLinks",
                    "Verses",
                    "Surahs",
                    "LLMModels",
                    "LLMProviders",
                    "SyncState"
                };

                foreach (var table in tables)
                {
                    cmd.CommandText = $"DELETE FROM {table};";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{table}';";
                    try { cmd.ExecuteNonQuery(); } catch { }
                }

                cmd.CommandText = "PRAGMA foreign_keys = ON;";
                cmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<Verse> GetVersesBySurah(int surahNumber)
        {
            var verses = new List<Verse>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Verses WHERE SurahId = @surahId ORDER BY VerseNumber";
            command.Parameters.AddWithValue("@surahId", surahNumber);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                verses.Add(ReadVerse(reader));
            }
            return verses;
        }

        public int AddLLMProvider(string providerName, string? apiKey = null, string? baseUrl = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO LLMProviders (ProviderName, ApiKey, BaseUrl)
                VALUES (@providerName, @apiKey, @baseUrl)
                ON CONFLICT(ProviderName) DO UPDATE SET ApiKey = @apiKey, BaseUrl = @baseUrl;
                SELECT Id FROM LLMProviders WHERE ProviderName = @providerName;";
            command.Parameters.AddWithValue("@providerName", providerName);
            command.Parameters.AddWithValue("@apiKey", apiKey ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@baseUrl", baseUrl ?? (object)DBNull.Value);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int AddLLMModel(int providerId, string modelName, string displayName, bool isFree = true)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO LLMModels (ProviderId, ModelName, DisplayName, IsFree)
                VALUES (@providerId, @modelName, @displayName, @isFree)
                ON CONFLICT(ProviderId, ModelName) DO UPDATE SET DisplayName = @displayName, IsFree = @isFree;
                SELECT Id FROM LLMModels WHERE ProviderId = @providerId AND ModelName = @modelName;";
            command.Parameters.AddWithValue("@providerId", providerId);
            command.Parameters.AddWithValue("@modelName", modelName);
            command.Parameters.AddWithValue("@displayName", displayName);
            command.Parameters.AddWithValue("@isFree", isFree ? 1 : 0);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public List<LLMModelInfo> GetActiveLLMModels()
        {
            var models = new List<LLMModelInfo>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT m.Id, m.ModelName, m.DisplayName, p.ProviderName, p.ApiKey, p.BaseUrl, m.IsFree
                FROM LLMModels m
                JOIN LLMProviders p ON m.ProviderId = p.Id
                WHERE m.IsActive = 1 AND p.IsActive = 1";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                models.Add(new LLMModelInfo
                {
                    Id = reader.GetInt32(0),
                    ModelName = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    ProviderName = reader.GetString(3),
                    ApiKey = reader.IsDBNull(4) ? null : reader.GetString(4),
                    BaseUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsFree = reader.GetInt32(6) == 1
                });
            }
            return models;
        }

        public LLMModelInfo? GetLLMModel(int modelId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT m.Id, m.ModelName, m.DisplayName, p.ProviderName, p.ApiKey, p.BaseUrl, m.IsFree
                FROM LLMModels m
                JOIN LLMProviders p ON m.ProviderId = p.Id
                WHERE m.Id = @modelId";
            command.Parameters.AddWithValue("@modelId", modelId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new LLMModelInfo
                {
                    Id = reader.GetInt32(0),
                    ModelName = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    ProviderName = reader.GetString(3),
                    ApiKey = reader.IsDBNull(4) ? null : reader.GetString(4),
                    BaseUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsFree = reader.GetInt32(6) == 1
                };
            }
            return null;
        }

        public void AddAIInterpretation(int verseId, int modelId, string interpretation, int costTokens = 0)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO AIInterpretations (VerseId, ModelId, Interpretation, GeneratedAt, CostTokens)
                VALUES (@verseId, @modelId, @interpretation, @generatedAt, @costTokens)";
            command.Parameters.AddWithValue("@verseId", verseId);
            command.Parameters.AddWithValue("@modelId", modelId);
            command.Parameters.AddWithValue("@interpretation", interpretation);
            command.Parameters.AddWithValue("@generatedAt", DateTime.Now);
            command.Parameters.AddWithValue("@costTokens", costTokens);
            command.ExecuteNonQuery();
        }

        public List<AIInterpretation> GetAIInterpretations(int verseId)
        {
            var interpretations = new List<AIInterpretation>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT i.Id, i.VerseId, m.ModelName, i.Interpretation, i.GeneratedAt, i.CostTokens
                FROM AIInterpretations i
                JOIN LLMModels m ON i.ModelId = m.Id
                WHERE i.VerseId = @verseId";
            command.Parameters.AddWithValue("@verseId", verseId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                interpretations.Add(new AIInterpretation
                {
                    Id = reader.GetInt32(0),
                    VerseId = reader.GetInt32(1),
                    ModelName = reader.GetString(2),
                    Interpretation = reader.GetString(3),
                    GeneratedAt = reader.GetDateTime(4),
                    CostTokens = reader.GetInt32(5)
                });
            }
            return interpretations;
        }

        public AIInterpretation? GetAIInterpretation(int verseId, int modelId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT i.Id, i.VerseId, m.ModelName, i.Interpretation, i.GeneratedAt, i.CostTokens
                FROM AIInterpretations i
                JOIN LLMModels m ON i.ModelId = m.Id
                WHERE i.VerseId = @verseId AND i.ModelId = @modelId";
            command.Parameters.AddWithValue("@verseId", verseId);
            command.Parameters.AddWithValue("@modelId", modelId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new AIInterpretation
                {
                    Id = reader.GetInt32(0),
                    VerseId = reader.GetInt32(1),
                    ModelName = reader.GetString(2),
                    Interpretation = reader.GetString(3),
                    GeneratedAt = reader.GetDateTime(4),
                    CostTokens = reader.GetInt32(5)
                };
            }
            return null;
        }

        public void SaveInterpretationToFile(int surahNumber, int modelId, string content)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "QuranData", "Interpretations");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            var filePath = Path.Combine(dataDir, $"surah_{surahNumber}_model_{modelId}.json");

            var interpretationData = new
            {
                SurahNumber = surahNumber,
                ModelId = modelId,
                GeneratedAt = DateTime.Now,
                Content = content
            };

            File.WriteAllText(filePath, System.Text.Json.JsonSerializer.Serialize(interpretationData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO FileBackups (SurahNumber, ModelId, FilePath, CreatedAt)
                VALUES (@surahNumber, @modelId, @filePath, @createdAt)";
            command.Parameters.AddWithValue("@surahNumber", surahNumber);
            command.Parameters.AddWithValue("@modelId", modelId);
            command.Parameters.AddWithValue("@filePath", filePath);
            command.Parameters.AddWithValue("@createdAt", DateTime.Now);
            command.ExecuteNonQuery();
        }

        public string? LoadInterpretationFromFile(int surahNumber, int modelId)
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "QuranData", "Interpretations");
            var filePath = Path.Combine(dataDir, $"surah_{surahNumber}_model_{modelId}.json");

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            return null;
        }

        public void SetSyncState(string key, string value)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO SyncState (Key, Value, UpdatedAt)
                VALUES (@key, @value, @updatedAt)
                ON CONFLICT(Key) DO UPDATE SET Value = @value, UpdatedAt = @updatedAt";
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            command.ExecuteNonQuery();
        }

        public string? GetSyncState(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM SyncState WHERE Key = @key";
            command.Parameters.AddWithValue("@key", key);

            var result = command.ExecuteScalar();
            return result?.ToString();
        }
    }

    public class LLMModelInfo
    {
        public int Id { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public string? BaseUrl { get; set; }
        public bool IsFree { get; set; }
    }
}