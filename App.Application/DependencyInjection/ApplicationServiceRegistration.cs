using App.Application.Behaviors;
using NuReaper.Application.Commands.ScanPackage;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(ScanPackageCommand).Assembly));

            services.AddValidatorsFromAssemblyContaining<ScanPackageCommandValidator>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // services.AddAutoMapper(cfg => cfg.AddMaps(typeof(UserMappingProfile).Assembly));

            return services;
        }
    }
}
