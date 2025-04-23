namespace DUPPA.Database
{
    using DUPPA.Models;
    using Npgsql;
    using System;
    using System.Collections.Generic;

    public class DataBase
    {
        private static DataBase? _instance;

        private string ConnectionString = "Host=yamabiko.proxy.rlwy.net;Port=29336;Username=postgres;Password=rkqbGBDrZPduWErOfBoVjbLbGIaVYcjX;Database=railway;SSL Mode=Require;Trust Server Certificate=true";

        public Dictionary<string, string> _defaultUserForDay;

        private DataBase()
        {
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
            var names = new[] { "Jan", "Jeremi", "Jerzy", "Ryszard" };
            _defaultUserForDay = new Dictionary<string, string>();

            foreach (var name in names)
            {
                AddUser(name);
            }

            _defaultUserForDay.Add("Sunday", "Jerzy");
            _defaultUserForDay.Add("Monday", "Jan");
            _defaultUserForDay.Add("Tuesday", "Jeremi");
            _defaultUserForDay.Add("Wednesday", "Ryszard");
            _defaultUserForDay.Add("Thursday", "Jan");
            _defaultUserForDay.Add("Friday", "Jeremi");
            _defaultUserForDay.Add("Saturday", "Ryszard");
        }

        public void AddUser(string name)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO users (name) VALUES (@name) ON CONFLICT (name) DO NOTHING;";
            command.Parameters.AddWithValue("@name", name);
            command.ExecuteNonQuery();
        }

        public List<User> GetUsers()
        {
            var users = new List<User>();
            using var connection = new NpgsqlConnection(ConnectionString);
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

        public void AddScoreForDay(string userName, int score, DateTime date, string? note)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var getUserIdCommand = connection.CreateCommand();
            getUserIdCommand.CommandText = "SELECT id FROM users WHERE name = @name;";
            getUserIdCommand.Parameters.AddWithValue("@name", userName);
            var userId = getUserIdCommand.ExecuteScalar();

            if (userId == null)
                return;

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO scores (date, user_id, score, note)
                VALUES (@date, @user_id, @score, @note)
                ON CONFLICT (date) DO UPDATE
                SET user_id = EXCLUDED.user_id,
                    score = EXCLUDED.score,
                    note = EXCLUDED.note;
            ";

            insertCommand.Parameters.AddWithValue("@date", date);
            insertCommand.Parameters.AddWithValue("@user_id", (int)userId);
            insertCommand.Parameters.AddWithValue("@score", score);
            insertCommand.Parameters.AddWithValue("@note", note ?? (object)DBNull.Value);

            insertCommand.ExecuteNonQuery();
        }

        private List<Score> GetScoresByUserId(int userId)
        {
            var scores = new List<Score>();

            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var getScoresCommand = connection.CreateCommand();
            getScoresCommand.CommandText = "SELECT score, date, note FROM scores WHERE user_id = @user_id;";
            getScoresCommand.Parameters.AddWithValue("@user_id", userId);

            using var reader = getScoresCommand.ExecuteReader();
            while (reader.Read())
            {
                var value = reader.GetInt32(0);
                var date = reader.GetDateTime(1);
                string? note = reader.IsDBNull(2) ? null : reader.GetString(2);
                scores.Add(new Score(value, date, note));
            }

            return scores;
        }

        public List<(DateTime CreatedAt, string UserName, int Value, string? Note)> GetAllScores()
        {
            var scores = new List<(DateTime, string, int, string?)>();

            using var connection = new NpgsqlConnection(ConnectionString);
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
                var date = reader.GetDateTime(0);
                var name = reader.GetString(1);
                var score = reader.GetInt32(2);
                var note = reader.IsDBNull(3) ? null : reader.GetString(3);

                scores.Add((date, name, score, note));
            }

            return scores;
        }
    }
}
