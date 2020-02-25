using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    internal interface IExecutor
    {
        Task Execute(long data, CancellationToken cancellationToken);
    }
}
