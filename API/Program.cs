using API.Data;
using API.Repositories;
using Microsoft.EntityFrameworkCore;
using ServiceBusUtils;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<CoreDbSettings>();
            builder.Services.AddDbContext<CoreDbContext>();

            builder.Services.AddSingleton<IServiceBusSettings, AzureServiceBusSettings>();
            builder.Services.AddSingleton<ServiceBusPublisher>(); // consumer will be created on hosted service level

            // same visibility as in dbcontext
            builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

                // TODO: add more complex logic
                context.Database.EnsureCreated(); // ensure db exists
                context.Database.Migrate(); // create tables
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}