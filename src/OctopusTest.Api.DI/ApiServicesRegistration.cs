using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OctopusTest.Api.Settings;

namespace OctopusTest.Api.DI
{
    public static class ApiServicesRegistration
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services,
                                                        IConfiguration configuration)
        {
            services.Configure<ApiSettings>(configuration.GetSection(nameof(ApiSettings)));

            return services;
        }
    }
}