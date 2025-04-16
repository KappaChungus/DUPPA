namespace DUPPA.Database
{
    using DUPPA.Models;
    using Microsoft.Data.Sqlite;
    using System;
    using System.Collections.Generic;

    public class DataBase
    {
        private static DataBase? _instance;
        private const string ConnectionString = "Data Source=./database/duppa.db";
        public Dictionary<string, string> _defaultUserForDay;

        private DataBase()
        {
            InitializeDatabase();
            SeedDefaultUsers();
        }

        public static DataBase Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DataBase();
                return _instance;
            }
        }
        
        public void SeedDefaultUsers()
        {
            var names = new[] { "Jan", "Jeremiasz", "Jerzy", "Ryszard" };
             _defaultUserForDay = new Dictionary<string,string>();

            foreach (var name in names)
            {
                AddUser(name);
            }
            _defaultUserForDay.Add("Sunday", "Jerzy");
            _defaultUserForDay.Add("Monday", "Jan");
            _defaultUserForDay.Add("Tuesday", "Jeremiasz");
            _defaultUserForDay.Add("Wednesday", "Ryszard");
            _defaultUserForDay.Add("Thursday", "Jan");
            _defaultUserForDay.Add("Friday", "Ryszard");
            _defaultUserForDay.Add("Saturday", "Jeremiasz");
        }

        
        public void AddUser(string name)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO users (name) VALUES ($name);";
            command.Parameters.AddWithValue("$name", name);
            command.ExecuteNonQuery();

            Console.WriteLine($"User '{name}' added.");
        }


        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS scores (
                    date TEXT PRIMARY KEY,
                    user_id INTEGER NOT NULL,
                    score INTEGER NOT NULL,
                    note TEXT,
                    FOREIGN KEY (user_id) REFERENCES users(id)
                );
            ";

            command.ExecuteNonQuery();
        }


        // Get all users with their scores
        public List<User> GetUsers()
        {
            var users = new List<User>();
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var getUsersCommand = connection.CreateCommand();
            getUsersCommand.CommandText = "SELECT id, name FROM users;";

            using var reader = getUsersCommand.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);
                var scores = GetScoresByUserId(id);

                users.Add(new User(id, name, scores));
            }

            return users;
        }

        // Add a new score for a user
        public void AddScoreForDay(string userName, int score, DateTime date, string? note)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var getUserIdCommand = connection.CreateCommand();
            getUserIdCommand.CommandText = "SELECT id FROM users WHERE name = $name;";
            getUserIdCommand.Parameters.AddWithValue("$name", userName);
            var userId = getUserIdCommand.ExecuteScalar();

            if (userId == null)
            {
                Console.WriteLine($"User '{userName}' not found.");
                return;
            }

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
        INSERT OR REPLACE INTO scores (date, user_id, score, note)
        VALUES (date($date), $user_id, $score, $note);";

            insertCommand.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
            insertCommand.Parameters.AddWithValue("$user_id", (long)userId);
            insertCommand.Parameters.AddWithValue("$score", score);
            insertCommand.Parameters.AddWithValue("$note", note ?? (object)DBNull.Value);

            try
            {
                insertCommand.ExecuteNonQuery();
                Console.WriteLine($"Score {score} added for {userName} on {date.ToShortDateString()} with note '{note}'.");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                Console.WriteLine($"A score already exists for {date.ToShortDateString()}.");
            }
        }



        // Get scores by user ID (private helper)
        private List<Score> GetScoresByUserId(int userId)
        {
            var scores = new List<Score>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var getScoresCommand = connection.CreateCommand();
            getScoresCommand.CommandText = "SELECT score, date, note FROM scores WHERE user_id = $user_id;";
            getScoresCommand.Parameters.AddWithValue("$user_id", userId);

            using var reader = getScoresCommand.ExecuteReader();
            while (reader.Read())
            {
                var value = reader.GetInt32(0);
                var dateStr = reader.GetString(1);
                var date = DateTime.Parse(dateStr);
                string? note = reader.IsDBNull(2) ? null : reader.GetString(2);
                scores.Add(new Score(value, date, note));
            }

            return scores;
        }
        
        public List<(DateTime CreatedAt, string UserName, int Value, string? Note)> GetAllScores()
        {
            var scores = new List<(DateTime, string, int, string?)>();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
        SELECT s.date, u.name, s.score, s.note
        FROM scores s
        JOIN users u ON s.user_id = u.id;
    ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var date = DateTime.Parse(reader.GetString(0));
                var name = reader.GetString(1);
                var score = reader.GetInt32(2);
                var note = reader.IsDBNull(3) ? null : reader.GetString(3);

                scores.Add((date, name, score, note));
            }

            return scores;
        }

    }
}
