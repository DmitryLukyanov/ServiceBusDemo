using System.Text.Json.Serialization;

namespace Models
{
    public sealed class HistoryModel
    {
        public HistoryModel(
            Guid id,
            string query,
            DateTime created,
            string? userName,
            TimeSpan? duration,
            DateTime? completed) 
            : this(id, query, created, resultLink: null, userName, duration, completed)
        {
        }

        [JsonConstructor]
        public HistoryModel(
            Guid id, 
            string query, 
            DateTime created, 
            Uri? resultLink, 
            string? userName, 
            TimeSpan? duration, 
            DateTime? completed)
        {
            Id = id;
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Created = created;
            ResultLink = resultLink; // can be null
            UserName = userName; // can be null until we enable authorization
            Duration = duration; // can be null
            Completed = completed; // can be null
        }

        public DateTime Created { get; }
        public DateTime? Completed { get; }
        public TimeSpan? Duration { get; }
        public Guid Id { get; }
        public string Query { get; }
        public Uri? ResultLink { get; }
        public string? UserName { get; }
    }

}
