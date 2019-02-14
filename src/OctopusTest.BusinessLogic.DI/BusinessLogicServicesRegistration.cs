using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OctopusTest.BusinessLogic.Settings;

namespace OctopusTest.BusinessLogic.DI
{
    public static class BusinessLogicServicesRegistration
    {
        public static IServiceCollection AddBusinessLogicServices(this IServiceCollection services,
                                                                  IConfiguration configuration)
        {
            services.Configure<BusinessLogicSettings>(configuration.GetSection(nameof(BusinessLogicSettings)));

            return services;
        }
    }
}