using System.Text.Json.Serialization;

namespace Models
{
    [method: JsonConstructor]
    public sealed class HistoryModel(
        Guid id,
        string query,
        DateTime created,
        Uri? resultLink,
        string? userName,
        TimeSpan? duration,
        DateTime? completed,
        DateTime sentAt)
    {
        public HistoryModel(
            Guid id,
            string query,
            DateTime created,
            string? userName,
            DateTime sentAt) 
            : this(id, query, created, resultLink: null, userName, duration: null, completed: null, sentAt)
        {
        }

        public DateTime Created { get; } = created;
        public DateTime? Completed { get; } = completed; // can be null
        public TimeSpan? Duration { get; } = duration; // can be null
        public Guid Id { get; } = id;
        public string Query { get; } = query ?? throw new ArgumentNullException(nameof(query));
        public Uri? ResultLink { get; } = resultLink; // can be null
        public DateTime SentAt { get; } = sentAt;
        public string? UserName { get; } = userName; // can be null until we enable authorization
    }
}
