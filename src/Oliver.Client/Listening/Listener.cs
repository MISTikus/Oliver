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
                foreach (var instance in options.Instances)
                {
                    var request = new RestRequest($"api/exec/{instance.Tenant}/{instance.Environment}/check")
                    {
                        Timeout = 10 * 60 * 1000
                    };

                    var response = await this.restClient.ExecuteAsync<long>(request, cancellationToken: cancellationToken);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var executor = this.executorFactory();
                        executor.Execute(instance, response.Data, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        this.logger.LogWarning($"Response status code: {response.StatusCode}.\n" +
                            $"Response: {response.Content}");
                    }
                }
                await Task.Delay(3000);
            }
            this.logger.LogInformation("Stop listening...");
        }

        public async Task StopAsync(CancellationToken cancellationToken) { }
    }
}
