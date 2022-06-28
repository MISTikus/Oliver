using static Oliver.Client.Configurations.Client;

namespace Oliver.Client.Executing;

internal interface IExecutor
{
    Task ExecuteAsync(Instance instance, long data, CancellationToken cancellationToken);
}
