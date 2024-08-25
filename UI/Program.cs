using Polly;
using UI.Middlewares;
using UI.Settings;
using UI.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<HttpClientContainer>((serviceProvider, client) => 
    {
        var settings = serviceProvider.GetRequiredService<APISettings>();
        client.BaseAddress = new Uri(settings.APIHostAddress);
    })
    .AddPolicyHandler((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<APISettings>();
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(settings.APITimeoutSeconds));
    });
builder.Services.AddSingleton<SignalRSettings>();
builder.Services.AddSingleton<APISettings>();
builder.Services.AddTransient<SetConfigurationToCookiesMiddleware>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<SetConfigurationToCookiesMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
