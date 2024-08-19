using BackgroundWorker.Data;
using BackgroundWorker.HostedServices;
using BackgroundWorker.Repositories;
using BackgroundWorker.SignalR;
using BackgroundWorker.Utils;
using Microsoft.Extensions.Azure;
using ServiceBusUtils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<ServiceBusHostedService>();
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(option =>
{
    option.AddPolicy(name: "CorsOrigins", policy =>
    {
        policy.WithOrigins("https://localhost:53630")
        .WithHeaders("x-requested-with", "x-signalr-user-agent")
        .AllowCredentials();
    });
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
app.UseCors("CorsOrigins");
app.MapHub<NotificationHub>("/NotificationHub");

app.Run();