using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using AccountTransfer.Interfaces;
using Orleans.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace OrleansClient
{
    /// <summary>
    /// Orleans test silo client
    /// </summary>
    public class Program
    {
        private static IConfigurationRoot configuration;

        static int Main(string[] args)
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

                using (var client = await StartClientWithRetries())
                {
                    DoClientWork(client);
                    Console.ReadKey();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            int attempt = 0;
            IClusterClient client;
            while (true)
            {
                try
                {
                    client = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "AccountTransferApp";
                        })
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IAccountGrain).Assembly).WithReferences())
                        .ConfigureLogging(logging => logging.AddConsole())
                        .Build();

                    await client.Connect();
                    Console.WriteLine("Client successfully connect to silo host");
                    break;
                }
                catch (SiloUnavailableException)
                {
                    attempt++;
                    Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
            }

            return client;
        }

        private static void DoClientWork(IClusterClient client)
        {
            var subscriptionClient = new SubscriptionClient(
                configuration.GetConnectionString("ServiceBusConnectionString"),
                "accountTransferUpdates",
                "orleansSubscription",
                ReceiveMode.ReceiveAndDelete);
            subscriptionClient.PrefetchCount = 1000;

            try
            {
                subscriptionClient.RegisterMessageHandler(
                    async (message, token) =>
                    {
                        var messageJson = Encoding.UTF8.GetString(message.Body);
                        var updateMessage = JsonConvert.DeserializeObject<AccountTransferMessage>(messageJson);

                        await client.GetGrain<IAccountGrain>(updateMessage.From).Withdraw(updateMessage.Amount);
                        await client.GetGrain<IAccountGrain>(updateMessage.To).Deposit(updateMessage.Amount);
                        
                        Console.WriteLine($"Processed a message from {updateMessage.From} to {updateMessage.To}");
                        await Task.CompletedTask;
                    },
                    new MessageHandlerOptions(HandleException)
                    {
                        MaxAutoRenewDuration = TimeSpan.FromMinutes(60),
                        MaxConcurrentCalls = 20,
                        AutoComplete = true
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }

        private static Task HandleException(ExceptionReceivedEventArgs args)
        {
            Console.WriteLine(args.Exception + ", stack trace: " + args.Exception.StackTrace);
            return Task.CompletedTask;
        }
    }
}
