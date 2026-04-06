using AuthApi.Application.Common;
using AuthApi.Application.Common.Behavior;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.Application.Configuration
{
    public static class DependencyInjection
    {
        // Config DI Application Layer
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var currentAssembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(config => config.RegisterServicesFromAssembly(currentAssembly));

            services.AddValidatorsFromAssembly(currentAssembly);

            #region Register Pipeline Behavior
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssemblyContaining<ApplicationAssembly>()
             );

            services.AddValidatorsFromAssemblyContaining<ApplicationAssembly>();

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            #endregion

            return services;
        }
    }
}