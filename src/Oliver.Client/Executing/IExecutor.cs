using System.Threading;
using System.Threading.Tasks;
using static Oliver.Client.Configurations.Client;

namespace Oliver.Client.Executing
{
    internal interface IExecutor
    {
        Task Execute(Instance instance, long data, CancellationToken cancellationToken);
    }
}
