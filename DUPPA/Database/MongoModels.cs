
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DUPPA.Database
{
    // This class maps to the 'users' collection
    internal class MongoUser
    {
        [BsonId] // We will control this integer ID
        public int Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }
    }

    // This class maps to the 'scores' collection
    internal class MongoScore
    {
        [BsonId] // The date is the unique primary key
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc, DateOnly = true)]
        public DateTime Date { get; set; }

        [BsonElement("user_id")]
        public int UserId { get; set; }

        [BsonElement("score")]
        public int Value { get; set; }

        [BsonElement("note")]
        public string? Note { get; set; }
    }

    // This class is for the 'counters' collection to get auto-incrementing IDs
    internal class Counter
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("sequence_value")]
        public int SequenceValue { get; set; }
    }
}