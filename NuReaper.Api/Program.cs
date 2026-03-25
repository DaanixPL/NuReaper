using NuReaper.Api.Middleware;
using NuReaper.Application.Behaviors;
using NuReaper.Application.DependencyInjection;
using NuReaper.Infrastructure.DependencyInjection;
using MediatR;
using Serilog;

namespace NuReaper.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);

            builder.Services.AddSwaggerServices();
            builder.Services.AddHttpClient("NuReaperClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("NuReaperScanner/1.0");
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 50
            });
            // builder.Services.AddAuthorization();

            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug ,outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 14,
                    shared: true)
                .CreateLogger();

            builder.Host.UseSerilog();

            try
            {
                Log.Information("--- Starting NuReaper API ---");
                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                // app.UseAuthentication();
                // app.UseAuthorization();
                app.UseCors("AllowAllOrigins");

                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
