namespace DUPPA.Database
{
    public class Score
    {
        public int Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note;

        public Score(int value, DateTime createdAt, string? note = null)
        {
            Value = value;
            CreatedAt = createdAt;
            Note = note;
        }
    }
}