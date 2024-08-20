namespace UI.Settings
{
    public sealed class APISettings(IConfiguration _configuration)
    {
        public string APIHostAddress => _configuration.GetValue<string>(nameof(APIHostAddress)) ?? throw new ArgumentNullException(nameof(APIHostAddress));
    }
}
