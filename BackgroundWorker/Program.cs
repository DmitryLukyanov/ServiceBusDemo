using BackgroundWorker.Data;
using BackgroundWorker.HostedServices;
using BackgroundWorker.Repositories;
using BackgroundWorker.SignalR;
using BackgroundWorker.Utils;
using Microsoft.Extensions.Azure;
using Serilog;
using ServiceBusUtils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<ServiceBusHostedService>();
builder.Services.AddHostedService<DeadLetterProcessingHostedService>();
builder.Services.AddControllers();
builder.Services.AddSingleton<CoreDbSettings>();
builder.Services.AddDbContext<CoreDbContext>();
builder.Services.AddSingleton<IServiceBusSettings, AzureServiceBusSettings>();
builder.Services.AddSingleton<BackgroundWorkerSettings>();
builder.Services.AddScoped<ILongRunningOperationRepository, LongRunningOperationRepository>();
builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddAzureClients(clientBuilder =>
{
    // See for more details: https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection?tabs=web-app-builder

    // TODO: try to resolve BackgroundWorkerSettings directly instead builder.Configuration.GetValue approach
    clientBuilder
        .AddBlobServiceClient(builder.Configuration.GetValue<string>(nameof(BackgroundWorkerSettings.BlobConnectionString)))
        // .WithCredential() 
        ;
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<SignalRUtils>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy(name: "CorsSignalr", policyBuilder =>
            policyBuilder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowed(hostName => true));
    }
    else
    {
        var backgroundWorkerSettings = new BackgroundWorkerSettings(builder.Configuration); // TODO: resolve via DI?
        options.AddPolicy(name: "CorsSignalr", policyBuilder =>
            policyBuilder
                .WithOrigins(backgroundWorkerSettings.AllowedProductionOrigins.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .WithHeaders("x-requested-with", "x-signalr-user-agent")
                .AllowCredentials());
    }
});

builder.Host.UseSerilog((context, serviceProvider, config) =>
{
    var settings = serviceProvider.GetRequiredService<BackgroundWorkerSettings>();
    if (!string.IsNullOrWhiteSpace(settings.LogFile))
    {
        config.MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(settings.LogFile, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseCors("CorsSignalr");
app.MapHub<NotificationHub>("/NotificationHub");

app.Run();