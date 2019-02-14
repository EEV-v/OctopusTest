using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using R1.AuthenticationService.ClientV2.DI;
using R1.Common.AppHosting;
using R1.Common.RequestLogging.LoggingStrategy;
using R1.HealthCheck.Extensions;
using R1.HealthCheck.MvcCore;
using R1.SecurityScheme.MvcCore.DefaultSchemes;
using R1.ServiceDiscovery.AppHosting.Context;
using R1.ServiceDiscovery.AppHosting.DI;
using OctopusTest.Api.DI;
using OctopusTest.BusinessLogic.DI;

namespace OctopusTest.Host
{
    public class Startup : AppStartupBase
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            IServiceDiscoveryContext discoveryContext)
        {
            app.UseSwaggerDevelopmentPage();
            app.UseServiceDiscoveryBasePath(discoveryContext);
            app.UseRequestMetrics();
            app.UseRequestLogging();
            app.UseMvc();
        }

        protected override void RegisterAppServices(IServiceCollection services)
        {
            base.RegisterAppServices(services);

            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddApplicationPart(typeof(HealthCheckController).Assembly)
                     // TODO: Register ApplicationParts from RAML Assemblies
                    .AddControllersAsServices();

            services.AddMetrics();
            services.AddRequestLogging()
                    .AddNLogRequestLogger()
                    .AddClientRequestLogging()
                    .AddServerRequestLogging()
                    .WithHealthCheckExcludingFilter();

            services.AddApiServices(Configuration);
            services.AddBusinessLogicServices(Configuration);

            services.AddSwagger("OctopusTest");
        }

        protected override void RegisterAutofacModules(ContainerBuilder builder)
        {
            base.RegisterAutofacModules(builder);

            builder.RegisterModule<ServiceDiscoveryModule>();
            builder.RegisterModule<DefaultRaml2SecuritySchemesModule>();

            builder.RegisterHealthInfoProvider()
                   ;

            builder.RegisterV2AuthenticationModule()
                   .WithAccessTokenContextManager()
                   .RegisterAuthenticationConfigFromConfiguration();
        }
    }
}