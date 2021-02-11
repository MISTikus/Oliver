using Microsoft.Extensions.Logging;
using Oliver.Common.Models;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    internal class LogSender : ILogSender
    {
        private readonly IRestClient restClient;
        private readonly ILogger<LogSender> logger;
        private readonly ConcurrentQueue<Action> queue;
        private readonly CancellationTokenSource cancellation;

        public LogSender(IRestClient restClient, ILogger<LogSender> logger)
        {
            this.restClient = restClient;
            this.logger = logger;
            this.queue = new ConcurrentQueue<Action>();
            this.cancellation = new CancellationTokenSource();
            Task.Run(QueueListenerAsync, this.cancellation.Token);
        }

        public Task LogStep(long executionId, int stepId, bool isLastStep = false, List<string> logs = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(() => this.cancellation.Cancel());

            this.queue.Enqueue(async () =>
            {
                var request = new RestRequest($"api/v1/executions/{executionId}", Method.PUT);

                if (isLastStep)
                    request.AddParameter("result", Execution.ExecutionState.Successed, ParameterType.QueryString);

                request.AddJsonBody(new Execution.StepState
                {
                    Executor = Environment.MachineName,
                    StepId = stepId,
                    IsSuccessed = true,
                    Log = logs
                });
                await this.restClient.ExecuteAsync(request, Method.PUT, cancellationToken);
            });

            this.logger.LogInformation($"ExecutionId: {executionId};\nStepId: {stepId}");
            this.logger.LogInformation(string.Join('\n', logs));
            return Task.CompletedTask;
        }

        public Task LogError(long executionId, string message, int stepId = 0, bool isLastStep = false, Exception error = null, List<string> logs = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(() => this.cancellation.Cancel());

            this.queue.Enqueue(async () =>
            {
                var request = new RestRequest($"api/v1/executions/{executionId}", Method.PUT);

                if (isLastStep)
                    request.AddParameter("result", Execution.ExecutionState.Failed, ParameterType.QueryString);
                logs = (logs ?? new List<string>()).Concat(new[] { message }).ToList();
                if (error != null)
                    logs = logs.Concat(new[] { error.Message, error.StackTrace }).ToList();

                request.AddJsonBody(new Execution.StepState
                {
                    Executor = Environment.MachineName,
                    StepId = stepId,
                    IsSuccessed = false,
                    Log = logs
                });

                await this.restClient.ExecuteAsync(request, Method.PUT, cancellationToken);
            });

            this.logger.LogWarning(error, $"{message}\nExecutionId: {executionId};\nStepId: {stepId}");
            return Task.CompletedTask;
        }

        private async Task QueueListenerAsync()
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                if (this.queue.TryDequeue(out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (System.Net.WebException e)
                    {
                        this.logger.LogWarning(e, "Error while trying to send step log...");
                        this.queue.Enqueue(action);
                    }
                    catch (Exception e)
                    {
                        this.logger.LogWarning(e, "Error while trying to send step log...");
                    }
                }
                await Task.Delay(100);
            }
        }
    }

    internal interface ILogSender
    {
        Task LogStep(long executionId, int stepId, bool isLastStep = false, List<string> logs = null, CancellationToken cancellationToken = default);
        Task LogError(long executionId, string message, int stepId = 0, bool isLastStep = false, Exception error = null, List<string> logs = null, CancellationToken cancellationToken = default);
    }
}
