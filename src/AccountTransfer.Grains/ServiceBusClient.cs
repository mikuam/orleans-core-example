using AccountTransfer.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace AccountTransfer.Grains
{
    public class ServiceBusClient : IServiceBusClient
    {
        private readonly TopicClient topicClient;

        public ServiceBusClient(string connectionString)
        {
            topicClient = new TopicClient(connectionString, "balanceUpdates");
        }

        public async Task SendMessageAsync(Message message)
        {
            await topicClient.SendAsync(message);
        }
    }
}
