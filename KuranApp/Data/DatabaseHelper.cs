using Microsoft.Data.Sqlite;
using KuranApp.Models;
using System.Collections.Generic;
using System;

namespace KuranApp.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=kuran.db";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Disable foreign key constraints for all connections on this database
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                cmd.ExecuteNonQuery();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE
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
                CREATE TABLE IF NOT EXISTS AIInterpretations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VerseId INTEGER,
                    ModelName TEXT,
                    Interpretation TEXT,
                    GeneratedAt DATETIME,
                    FOREIGN KEY(VerseId) REFERENCES Verses(Id)
                );
                CREATE TABLE IF NOT EXISTS SyncState (
                    Key TEXT PRIMARY KEY,
                    Value TEXT,
                    UpdatedAt DATETIME
                );
                CREATE TABLE IF NOT EXISTS Surahs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SurahNumber INTEGER UNIQUE,
                    Name TEXT,
                    EnglishName TEXT,
                    Meaning TEXT,
                    RevelationOrder INTEGER,
                    VerseCount INTEGER
                );
                CREATE TABLE IF NOT EXISTS UserSurahReads (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    SurahId INTEGER,
                    ReadAt DATETIME,
                    FOREIGN KEY(UserId) REFERENCES Users(Id),
                    FOREIGN KEY(SurahId) REFERENCES Surahs(Id),
                    UNIQUE(UserId, SurahId)
                );";
            command.ExecuteNonQuery();
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
                verses.Add(new Verse
                {
                    Id = reader.GetInt32(0),
                    SurahId = reader.GetInt32(1),
                    VerseNumber = reader.GetInt32(2),
                    RevelationOrder = reader.GetInt32(3),
                    ArabicText = reader.GetString(4),
                    TurkishTranslation = reader.GetString(5),
                    Summary = reader.GetString(6),
                    DownloadedAt = reader.GetDateTime(7)
                });
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
                return new Verse
                {
                    Id = reader.GetInt32(0),
                    SurahId = reader.GetInt32(1),
                    VerseNumber = reader.GetInt32(2),
                    RevelationOrder = reader.GetInt32(3),
                    ArabicText = reader.GetString(4),
                    TurkishTranslation = reader.GetString(5),
                    Summary = reader.GetString(6),
                    DownloadedAt = reader.GetDateTime(7)
                };
            }
            return null;
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
            command.Parameters.AddWithValue("@summary", verse.Summary);
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
            Console.WriteLine($"DB: Added Surah {surah.SurahNumber} with RevelationOrder {surah.RevelationOrder}");
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

        public void AddAIInterpretation(AIInterpretation interpretation)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO AIInterpretations (VerseId, ModelName, Interpretation, GeneratedAt)
                VALUES (@verseId, @modelName, @interpretation, @generatedAt)";
            
            command.Parameters.AddWithValue("@verseId", interpretation.VerseId);
            command.Parameters.AddWithValue("@modelName", interpretation.ModelName);
            command.Parameters.AddWithValue("@interpretation", interpretation.Interpretation);
            command.Parameters.AddWithValue("@generatedAt", interpretation.GeneratedAt);
            
            command.ExecuteNonQuery();
        }

        public List<AIInterpretation> GetAIInterpretations(int verseId)
        {
            var interpretations = new List<AIInterpretation>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM AIInterpretations WHERE VerseId = @verseId";
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
                    GeneratedAt = reader.GetDateTime(4)
                });
            }
            return interpretations;
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
                
                // Foreign keys geçici olarak kapatılır
                cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                cmd.ExecuteNonQuery();

                string[] tables = { 
                    "UserSurahReads", 
                    "UserReads", 
                    "AIInterpretations", 
                    "VerseLinks", 
                    "Verses", 
                    "Surahs", 
                    "SyncState" 
                };

                foreach (var table in tables)
                {
                    cmd.CommandText = $"DELETE FROM {table};";
                    cmd.ExecuteNonQuery();
                    
                    // Auto-increment counter'larını da sıfırlayalım
                    cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{table}';";
                    try { cmd.ExecuteNonQuery(); } catch { }
                }

                // Foreign keys tekrar açılır
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
        
        public void ClearTable(string tableName)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"DELETE FROM {tableName};";
            cmd.ExecuteNonQuery();
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
                verses.Add(new Verse
                {
                    Id = reader.GetInt32(0),
                    SurahId = reader.GetInt32(1),
                    VerseNumber = reader.GetInt32(2),
                    RevelationOrder = reader.GetInt32(3),
                    ArabicText = reader.GetString(4),
                    TurkishTranslation = reader.GetString(5),
                    Summary = reader.GetString(6),
                    DownloadedAt = reader.GetDateTime(7)
                });
            }
            return verses;
        }
    }
}
