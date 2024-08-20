using UI.Settings;

namespace UI.Middlewares
{
    public sealed class SetConfigurationToCookiesMiddleware(SignalRSettings _signalRSettings) : IMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.Request.Cookies.ContainsKey(nameof(SignalRSettings.SignalRHostAddress)))
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddYears(1),
                    HttpOnly = true, // Prevent client-side scripts from accessing the cookie
                    IsEssential = true, // Indicates the cookie is essential for the application to function
                    Secure = true // Https only
                };

                context.Response.Cookies.Append(
                    key: nameof(SignalRSettings.SignalRHostAddress),
                    _signalRSettings.SignalRHostAddress, 
                    cookieOptions);
            }

            return next(context);
        }
    }
}
