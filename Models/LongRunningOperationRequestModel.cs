namespace Models
{
    public sealed class LongRunningOperationRequestModel(Guid id, string query, DateTime created, string? userName)
    {
        private static string NormalizeQuery(string query) => query
            .Trim()
            .Replace(Environment.NewLine, " ")
            .Replace('\t', ' ')
            .Replace('\n', ' ')
            .Replace("  ", " ")
            .ToLowerInvariant();

        public Guid Id => id;
        public DateTime Created => created;
        /// <summary>
        /// Used as a key. TODO: think about better approach for cache key
        /// </summary>
        public string NormalizedQuery => NormalizeQuery(query);
        public string Query => query;
        public string? UserName => userName; // temporary allow 'string?' until auth is configured
    }
}
