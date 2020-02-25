using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oliver.Common.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    internal class Executor : IExecutor
    {
        private readonly IRestClient restClient;
        private readonly IOptions<Configurations.Instance> instanceOptions;
        private readonly ILogger<Executor> logger;

        public Executor(IRestClient restClient, IOptions<Configurations.Instance> instanceOptions, ILogger<Executor> logger)
        {
            this.restClient = restClient;
            this.instanceOptions = instanceOptions;
            this.logger = logger;
        }

        public async Task Execute(long executionId, CancellationToken cancellationToken)
        {
            var logs = new List<string>();
            try
            {

            }
            catch (Exception e)
            {
                await LogError(executionId, "Failed to execute", isLastStep: true, error: e, logs: logs.ToArray());
            }
        }

        private async Task LogError(long executionId, string message, int stepId = 0, bool isLastStep = false, Exception error = null, string[] logs = null)
        {
            var request = new RestRequest(executionId.ToString(), Method.PUT);

            if (isLastStep)
                request.AddParameter("result", Execution.ExecutionState.Failed);
            logs = (logs ?? new string[0]).Concat(new[] { message, error.Message, error.StackTrace }).ToArray();

            request.AddJsonBody(new Execution.StepState
            {
                Executor = Environment.MachineName,
                StepId = stepId,
                IsSuccessed = false,
                Log = logs
            });

            await this.restClient.ExecuteAsync(request, Method.PUT, CancellationToken.None);

            this.logger.LogWarning(error, $"{message}\nExecutionId: {executionId};\nStepId: {stepId}");
        }
    }
}
