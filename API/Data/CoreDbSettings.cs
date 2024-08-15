namespace API.Data
{
    public sealed class CoreDbSettings(IConfiguration configuration)
    {
        public string CoreConnectionString => configuration.GetValue<string>(nameof(CoreConnectionString)) ?? throw new ArgumentNullException(nameof(CoreConnectionString));
    }
}
