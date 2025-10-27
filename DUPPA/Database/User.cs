using DUPPA.Database;

namespace DUPPA.Models
{
    using System.Collections.Generic;

    public class User
    {
        public int Id { get; set; }           // Corresponds to 'id' in the 'users' table
        public string Name { get; set; }      // Corresponds to 'name' in the 'users' table
        public List<Score> Scores { get; set; }

        public User()
        {
            Scores = new List<Score>();
        }

        public User(int id, string name)
        {
            Id = id;
            Name = name;
            Scores = new List<Score>();
        }

        public User(int id, string name, List<Score> scores)
        {
            Id = id;
            Name = name;
            Scores = scores ?? new List<Score>();
        }
    }
}