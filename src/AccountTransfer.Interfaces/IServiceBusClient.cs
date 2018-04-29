using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace AccountTransfer.Interfaces
{
    public interface IServiceBusClient
    {
        Task SendMessageAsync(Message message);
    }
}
