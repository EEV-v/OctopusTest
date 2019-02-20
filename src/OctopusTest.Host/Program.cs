using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using R1.Common.AppHosting;
using R1.Common.AppHosting.Interfaces;
using R1.Common.AppHosting.WebHost;
using R1.ServiceDiscovery.AppHosting.DI;

namespace OctopusTest.Host
{
    public class Program
    {
        private static AppHostingEnvironment AppHostingEnvironment =>
#if DEBUG
            AppHostingEnvironment.Development;
#else
            AppHostingEnvironment.Production;
#endif

#pragma warning disable IDE1006 // Naming Styles
        public static void Main(string[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            return;
            //var host = CreateAppBuilder(args);

            //await host.AsApp()
            //          .WithDiscoveryRegistration()
            //          .RunAppAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            CreateAppBuilder(args)
               .WrappedHostBuilder;

        public static IAppHostBuilder<IWebHostBuilder> CreateAppBuilder(string[] args) =>
            WebHostBuilderFactory
               .CreateDefaultAppBuilder<Startup>(args,
                                                 AppHostingEnvironment)
               .ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddLocalConfiguration();
                });
    }
}