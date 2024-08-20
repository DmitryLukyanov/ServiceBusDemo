namespace Models
{
    public sealed class HistoryModel
    {
        public HistoryModel(Guid id, string query, DateTime created, string? userName, TimeSpan duration) : this(id, query, created, null, userName, duration)
        {
        }

        public HistoryModel(Guid id, string query, DateTime created, Uri? resultedLink, string? userName, TimeSpan duration)
        {
            Id = id;
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Created = created;
            ResultLink = resultedLink; // can be null
            UserName = userName; // can be null until we enable authorization
            Duration = duration;
        }

        public DateTime Created { get; }
        public TimeSpan Duration { get; }
        public Guid Id { get; }
        public string Query { get; }
        public Uri? ResultLink { get; }
        public string? UserName { get; }
    }

}
