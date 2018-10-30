using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Hosting.Development;
using Orleans.Configuration;
using System.Net;
using AccountTransfer.Grains;
using Microsoft.Extensions.DependencyInjection;
using AccountTransfer.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace OrleansSiloHost
{
    public class Program
    {
        private static IConfigurationRoot configuration;

        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                configuration = builder.Build();

                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureServices(context => ConfigureDI(context))
                .ConfigureLogging(logging => logging.AddConsole())
                .UseInClusterTransactionManager()
                .UseInMemoryTransactionLog()
                .AddAzureTableGrainStorageAsDefault(
                    (options) =>
                    {
                        options.ConnectionString = configuration.GetConnectionString("CosmosBDConnectionString");
                        options.UseJson = true;
                    })
                .UseTransactionalState();

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }

        private static IServiceProvider ConfigureDI(IServiceCollection services)
        {
            services.AddSingleton<IServiceBusClient>((sp) => new ServiceBusClient(configuration.GetConnectionString("ServiceBusConnectionString")));

            return services.BuildServiceProvider();
        }
    }
}
