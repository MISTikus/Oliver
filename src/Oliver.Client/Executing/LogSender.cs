using Microsoft.Extensions.Logging;
using Oliver.Client.Services;
using Oliver.Common.Models;
using System.Collections.Concurrent;

namespace Oliver.Client.Executing;

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
        queue = new ConcurrentQueue<Action>();
        cancellation = new CancellationTokenSource();
        Task.Run(QueueListenerAsync, cancellation.Token);
    }

    public Task LogStep(long executionId, int stepId, bool isLastStep = false, List<string> logs = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(() => cancellation.Cancel());

        queue.Enqueue(async () =>
        {
            await apiClient.SendExecutionLog(executionId, isLastStep, new Execution.StepState
            {
                Executor = Environment.MachineName,
                StepId = stepId,
                IsSuccess = true,
                Log = logs
            }, cancellationToken);
        });

        logger.LogInformation($"ExecutionId: {executionId};\nStepId: {stepId}");
        logger.LogInformation(string.Join('\n', logs));
        return Task.CompletedTask;
    }

    public Task LogError(long executionId, string message, int stepId = 0, bool isLastStep = false, Exception error = null, List<string> logs = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(() => cancellation.Cancel());

        queue.Enqueue(async () =>
        {
            logs ??= new List<string>();
            logs.Add(message);

            if (error != null)
            {
                logs.Add(error.Message);
                logs.Add(error.StackTrace);
            }

            await apiClient.SendExecutionLog(executionId, isLastStep, new Execution.StepState
            {
                Executor = Environment.MachineName,
                StepId = stepId,
                IsSuccess = false,
                Log = logs
            }, cancellationToken);
        });

        logger.LogWarning(error, $"{message}\nExecutionId: {executionId};\nStepId: {stepId}");
        return Task.CompletedTask;
    }

    private async Task QueueListenerAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            if (queue.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (System.Net.WebException e)
                {
                    logger.LogWarning(e, "Error while trying to send step log...");
                    queue.Enqueue(action);
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Error while trying to send step log...");
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
