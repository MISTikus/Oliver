using Microsoft.Extensions.Logging;
using Oliver.Client.Services;
using Oliver.Common.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    internal class LogSender : ILogSender
    {
        private readonly IApiClient apiClient;
        private readonly ILogger<LogSender> logger;
        private readonly ConcurrentQueue<Action> queue;
        private readonly CancellationTokenSource cancellation;

        public LogSender(IApiClient apiClient, ILogger<LogSender> logger)
        {
            this.apiClient = apiClient;
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
                await this.apiClient.SendExecutionLog(executionId, isLastStep, new Execution.StepState
                {
                    Executor = Environment.MachineName,
                    StepId = stepId,
                    IsSuccess = true,
                    Log = logs
                }, cancellationToken);
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
                logs ??= new List<string>();
                logs.Add(message);

                if (error != null)
                {
                    logs.Add(error.Message);
                    logs.Add(error.StackTrace);
                }

                await this.apiClient.SendExecutionLog(executionId, isLastStep, new Execution.StepState
                {
                    Executor = Environment.MachineName,
                    StepId = stepId,
                    IsSuccess = false,
                    Log = logs
                }, cancellationToken);
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
