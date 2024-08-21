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
        DateTime? completed)
    {
        public HistoryModel(
            Guid id,
            string query,
            DateTime created,
            string? userName) 
            : this(id, query, created, resultLink: null, userName, duration: null, completed: null)
        {
        }

        public DateTime Created { get; } = created;
        public DateTime? Completed { get; } = completed; // can be null
        public TimeSpan? Duration { get; } = duration; // can be null
        public Guid Id { get; } = id;
        public string Query { get; } = query ?? throw new ArgumentNullException(nameof(query));
        public Uri? ResultLink { get; } = resultLink; // can be null
        public string? UserName { get; } = userName; // can be null until we enable authorization
    }

}
