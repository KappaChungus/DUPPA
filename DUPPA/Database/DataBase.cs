using DUPPA.Models;
using MongoDB.Driver;
using System.Text.Json;

namespace DUPPA.Database
{
    public class DataBase
    {
        private static DataBase? _instance;
        private readonly string _connectionString = AppConfiguration.Configuration["MongoDb:MONGO_CONNECTION_STRING"]!;
        private readonly string _dbName = AppConfiguration.Configuration["MongoDb:DB_NAME"]!;

        public Dictionary<string, string>? DefaultUserForDay;

        private readonly IMongoCollection<MongoUser> _users;
        private readonly IMongoCollection<MongoScore> _scores;
        private readonly IMongoCollection<Counter> _counters;

        private DataBase()
        {
            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_dbName);
            _users = database.GetCollection<MongoUser>("users");
            _scores = database.GetCollection<MongoScore>("scores");
            _counters = database.GetCollection<Counter>("counters");
            
            SeedDefaultUsers();
        }

        public static DataBase Instance => _instance ?? (_instance = new DataBase());

        private int GetNextUserId()
        {
            var filter = Builders<Counter>.Filter.Eq(c => c.Id, "user_id");
            var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);
            var options = new FindOneAndUpdateOptions<Counter>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };
            var counter = _counters.FindOneAndUpdate(filter, update, options);
            return counter.SequenceValue;
        }

        private void SeedDefaultUsers()
        {
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexKeys = Builders<MongoUser>.IndexKeys.Ascending(u => u.Name);
            var indexModel = new CreateIndexModel<MongoUser>(indexKeys, indexOptions);
            _users.Indexes.CreateOne(indexModel);
            
            string json = File.ReadAllText("Database/defaultUserForDay.json");

            DefaultUserForDay = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;

            foreach (var kvp in DefaultUserForDay)
            {
                AddUser(kvp.Value);
            }
        }

        private void AddUser(string name)
        {
            var filter = Builders<MongoUser>.Filter.Eq(u => u.Name, name);
            if (_users.Find(filter).Any())
            {
                return;
            }

            var newUser = new MongoUser
            {
                Id = GetNextUserId(),
                Name = name
            };
            _users.InsertOne(newUser);
        }

        public List<User> GetUsers()
        {
            var mongoUsers = _users.Find(_ => true).ToList();
            var users = new List<User>();

            foreach (var mongoUser in mongoUsers)
            {
                var scores = GetScoresByUserId(mongoUser.Id);
                
                users.Add(new User(mongoUser.Id, mongoUser.Name, scores));
            }
            return users;
        }

        public void AddScoreForDay(string userName, int score, DateTime date, string? note)
        {
            var userFilter = Builders<MongoUser>.Filter.Eq(u => u.Name, userName);
            var user = _users.Find(userFilter).FirstOrDefault();

            if (user == null)
                return;
            var scoreDoc = new MongoScore
            {
                Date = date.Date,
                UserId = user.Id,
                Value = score,
                Note = note
            };
            var scoreFilter = Builders<MongoScore>.Filter.Eq(s => s.Date, date.Date);
            _scores.ReplaceOne(scoreFilter, scoreDoc, new ReplaceOptions { IsUpsert = true });
        }

        private List<Score> GetScoresByUserId(int userId)
        {
            var filter = Builders<MongoScore>.Filter.Eq(s => s.UserId, userId);
            var mongoScores = _scores.Find(filter).ToList();
            return mongoScores
                .Select(s => new Score(s.Value, s.Date, s.Note))
                .ToList();
        }

        public List<(DateTime CreatedAt, string UserName, int Value, string? Note)> GetAllScores()
        {
            var allScores = _scores.Find(_ => true).ToList();
            var allUsers = _users.Find(_ => true).ToList().ToDictionary(u => u.Id, u => u.Name);

            var result = new List<(DateTime, string, int, string?)>();
            foreach (var s in allScores)
            {
                if (allUsers.TryGetValue(s.UserId, out var userName))
                {
                    result.Add((s.Date, userName, s.Value, s.Note));
                }
            }
            return result;
        }
    }
}