namespace API.Data
{
    public sealed class CoreDbSettings(IConfiguration _configuration)
    {
        public string CoreConnectionString => _configuration.GetValue<string>(nameof(CoreConnectionString)) ?? throw new ArgumentNullException(nameof(CoreConnectionString));
    }
}
