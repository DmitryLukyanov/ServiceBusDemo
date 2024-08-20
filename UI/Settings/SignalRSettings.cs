namespace UI.Settings
{
    public class SignalRSettings(IConfiguration _configuration)
    {
        public string SignalRHostAddress => _configuration.GetValue<string>(nameof(SignalRHostAddress)) ?? throw new ArgumentNullException(nameof(SignalRHostAddress));
    }
}
