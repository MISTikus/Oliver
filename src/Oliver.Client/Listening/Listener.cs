using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oliver.Client.Executing;
using RestSharp;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Client.Listening
{
    internal class Listener : IHostedService
    {
        private readonly IRestClient restClient;
        private readonly IOptions<Configurations.Client> instanceOptions;
        private readonly Func<IExecutor> executorFactory;
        private readonly ILogger<Listener> logger;

        public Listener(IRestClient restClient, IOptions<Configurations.Client> instanceOptions, Func<IExecutor> executorFactory, ILogger<Listener> logger)
        {
            this.restClient = restClient;
            this.instanceOptions = instanceOptions;
            this.executorFactory = executorFactory;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Started listening for {this.restClient.BaseUrl}");
            while (!cancellationToken.IsCancellationRequested)
            {
                var options = this.instanceOptions.Value;
                var tasks = new Task[options.Instances.Length];
                for (var i = 0; i < options.Instances.Length; i++)
                {
                    var instance = options.Instances[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        var request = new RestRequest($"api/v1/executions/{instance.Tenant}/{instance.Environment}/check")
                        {
                            Timeout = 10 * 60 * 1000
                        };

                        var response = await this.restClient.ExecuteAsync<long>(request, cancellationToken: cancellationToken);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var executor = this.executorFactory();
                            await executor.ExecuteAsync(instance, response.Data, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            this.logger.LogWarning($"Instance: {instance.Tenant}.{instance.Environment}.\n" +
                                $"Response status code: {response.StatusCode}.\n" +
                                $"Response: {response.Content}");
                        }
                    }, cancellationToken);
                }
                await Task.WhenAll(tasks);
                await Task.Delay(3000, cancellationToken);
            }
            this.logger.LogInformation("Stop listening...");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
