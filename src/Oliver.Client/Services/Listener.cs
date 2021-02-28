using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oliver.Client.Executing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Client.Services
{
    internal class Listener : IHostedService
    {
        private readonly IApiClient apiClient;
        private readonly IOptions<Configurations.Client> instanceOptions;
        private readonly Func<IExecutor> executorFactory;
        private readonly ILogger<Listener> logger;

        public Listener(IApiClient apiClient, IOptions<Configurations.Client> instanceOptions, Func<IExecutor> executorFactory, ILogger<Listener> logger)
        {
            this.apiClient = apiClient;
            this.instanceOptions = instanceOptions;
            this.executorFactory = executorFactory;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Started listening for {this.apiClient.BaseUrl}");
            while (!cancellationToken.IsCancellationRequested)
            {
                var options = this.instanceOptions.Value;
                var tasks = new Task[options.Instances.Length];
                for (var i = 0; i < options.Instances.Length; i++)
                {
                    var instance = options.Instances[i];
                    this.logger.LogTrace($"Start executiong task for {instance.Tenant}.{instance.Environment}...");
                    tasks[i] = Task.Run(async () =>
                    {
                        var response = await this.apiClient.CheckExecutions(instance.Tenant, instance.Environment, cancellationToken);
                        if (response.HasValue)
                        {
                            var executor = this.executorFactory();
                            await executor.ExecuteAsync(instance, response.Value, cancellationToken).ConfigureAwait(false);
                        }
                    }, cancellationToken);
                }
                this.logger.LogTrace($"Waiting for {tasks.Length} tasks to finish...");
                await Task.WhenAll(tasks);
                await Task.Delay(3000, cancellationToken);
            }
            this.logger.LogInformation("Stop listening...");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
