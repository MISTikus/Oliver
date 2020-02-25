using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oliver.Client.Configurations;
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
        private readonly IOptions<Instance> instanceOptions;
        private readonly Func<IExecutor> executorFactory;
        private readonly ILogger<Listener> logger;

        public Listener(IRestClient restClient, IOptions<Instance> instanceOptions, Func<IExecutor> executorFactory, ILogger<Listener> logger)
        {
            this.restClient = restClient;
            this.instanceOptions = instanceOptions;
            this.executorFactory = executorFactory;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var options = this.instanceOptions.Value;
                var request = new RestRequest($"{options.Tenant}/{options.Environment}/check")
                {
                    Timeout = 10 * 60 * 1000
                };

                var response = await this.restClient.ExecuteAsync<long>(request, cancellationToken: cancellationToken);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var executor = this.executorFactory();
                    executor.Execute(response.Data, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    this.logger.LogWarning($"Response status code: {response.StatusCode}.\n" +
                        $"Response: {response.Content}");
                }

                await Task.Delay(3000);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken) { }
    }
}
