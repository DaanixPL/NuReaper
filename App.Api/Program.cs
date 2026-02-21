using App.Api.Middleware;
using App.Application.Behaviors;
using App.Application.DependencyInjection;
using NuReaper.Infrastructure.DependencyInjection;
using MediatR;

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
            // builder.Services.AddAuthorization();

            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

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
            app.MapControllers();

            app.Run();
        }
    }
}
