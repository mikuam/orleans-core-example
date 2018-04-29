using System.Threading.Tasks;
using Orleans;

namespace AccountTransfer.Interfaces
{
    public interface IATMGrain : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.RequiresNew)]
        Task Transfer(long fromAccount, long toAccount, decimal amountToTransfer);
    }
}
