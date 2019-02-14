using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using R1.ServiceDiscovery.AppHosting.UnitTest.DI;
using R1.UnitTest.ServiceProvider;

namespace OctopusTest.Host.Tests
{
    [TestClass]
    public class ApiServiceProviderTests : ServiceProviderTestBase
    {
        [TestMethod]
        public async Task ServiceProviderShouldResolveControllersAsync()
        {
            var configObj = new
            {
                // @formatter:off
                ConnectionStrings = new {
                  ElasticUri = "http://test-elk.trgdev.local:9200/"
                },
                AuthenticationService = new {
                  ClientId = "def0fa4e1e5166660000f41a33788d84",
                  Secret = "Ip8Z7n5rY0fbRkTdbM8tZOb8KUTgkYb9s6N37+Q5Uyg="
                },
                ConsulUrl = "http://localhost:8500",
                ServiceDiscovery = new {
                  Environment = "dev",
                  FallbackEnvironment = "dev01wb",
                  AutomaticUnregistrationEnabled = false,
                  UnregistrationTimeout = "",
                  ServiceBasePath = "",
                  Scheme = "http",
                  DynamicHosting = new {
                    PortRangeMin = 38822,
                    PortRangeMax = 38822
                  },
                  StaticHosting = new {
                    DiscoveryAddress = "",
                    DiscoveryPort = (int?)null,
                    HostingAddress = "",
                    HostingPort = (int?)null
                  }
                },
                DatabaseDiscovery = new {
                  Environment = "dev"
                },
                Elastic = new {
                  Index = new {
                    Prefix = ""
                  }
                },
                Stats = new {
                  Enabled = false
                },
                Statsd = new {
                  Metrics = new {
                    Prefix = ""
                  },
                  ServerName = "stat-dev.trgdev.local",
                  ServerPort = 8125
                },
                ApiSettings = new {
                },
                BusinessLogicSettings = new {
                },
                DataAccessSettings = new {
                }
                // @formatter:on
            };

            var configuration = new ConfigurationBuilder()
                               .AddInMemory(configObj)
                               .Build();

            var services = new ServiceCollection()
                          .AddSingleton<IConfiguration>(configuration)
                          .AddLogging(x => x.ClearProviders())
                          .AddOptions();

            using (var startUp = new Startup(configuration))
            {
                // ReSharper disable AccessToDisposedClosure
                var provider = startUp.ConfigureServicesTestServices(services, registerTestModules: x => x.RegisterModule<MockServiceDiscoveryModule>());
                // ReSharper restore AccessToDisposedClosure
                await SafeActAsync(() => Act<ControllerBase>(provider));
            }
        }
    }
}