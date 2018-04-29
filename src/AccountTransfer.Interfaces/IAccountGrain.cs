using System.Threading.Tasks;
using Orleans;

namespace AccountTransfer.Interfaces
{
    public interface IAccountGrain : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.Required)]
        Task Withdraw(decimal amount);

        [Transaction(TransactionOption.Required)]
        Task Deposit(decimal amount);

        [Transaction(TransactionOption.Required)]
        Task<decimal> GetBalance();
    }
}
