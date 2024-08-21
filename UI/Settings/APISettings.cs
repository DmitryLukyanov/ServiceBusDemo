namespace UI.Settings
{
    public sealed class APISettings(IConfiguration configuration)
    {
        public string APIHostAddress => configuration.GetValue<string>(nameof(APIHostAddress)) ?? throw new ArgumentNullException(nameof(APIHostAddress));
        public int APITimeoutSeconds => configuration.GetValue<int?>(nameof(APITimeoutSeconds)) ?? throw new ArgumentNullException(nameof(APITimeoutSeconds));
    }
}
