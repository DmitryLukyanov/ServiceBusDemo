namespace UI.Settings
{
    public sealed class SignalRSettings(IConfiguration configuration)
    {
        public string SignalRHostAddress => configuration.GetValue<string>(nameof(SignalRHostAddress)) ?? throw new ArgumentNullException(nameof(SignalRHostAddress));
    }
}
