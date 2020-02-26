using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oliver.Common.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    internal class Executor : IExecutor
    {
        private readonly IRestClient restClient;
        private readonly IOptions<Configurations.Instance> instanceOptions;
        private readonly ILogger<Executor> logger;
        private readonly IRunner runner;

        public Executor(IRestClient restClient, IOptions<Configurations.Instance> instanceOptions, ILogger<Executor> logger, IRunner runner)
        {
            this.restClient = restClient;
            this.instanceOptions = instanceOptions;
            this.logger = logger;
            this.runner = runner;
        }

        public async Task Execute(long executionId, CancellationToken cancellationToken)
        {
            try
            {
                var execution = await GetExecution(executionId, cancellationToken).ConfigureAwait(false);
                await Execute(execution, cancellationToken);
            }
            catch (Exception e)
            {
                await LogError(executionId, "Failed to execute", isLastStep: true, error: e);
            }
        }

        private async Task Execute(Execution execution, CancellationToken cancellationToken)
        {
            var template = await GetTemplate(execution.TemplateId, cancellationToken);
            var i = 1;
            foreach (var step in template.Steps.OrderBy(x => x.Order))
            {
                var logs = new List<string> { $"Starting step {step.Order}: '{step.Name}'" };

                // Builtin variables
                execution.Variables.Add(nameof(execution.Instance.Tenant), execution.Instance.Tenant);
                execution.Variables.Add(nameof(execution.Instance.Environment), execution.Instance.Environment);

                var command = Substitute(step.Command, execution.Variables);
                var folder = Substitute(step.WorkingFolder, execution.Variables);
                folder = Path.GetFullPath(folder);
                (bool isSuccessed, string[] logs) result = default;
                switch (step.Type)
                {
                    case Template.StepType.PShell:
                        result = await this.runner.RunPowerShell(folder, command);
                        break;
                    case Template.StepType.Docker:
                        result = await this.runner.RunDocker(folder, command);
                        break;
                    case Template.StepType.DockerCompose:
                        result = await this.runner.RunCompose(folder, command);
                        break;
                    default: throw new NotImplementedException();
                }
                if (!result.isSuccessed)
                    await LogError(execution.Id, $"Step '{step.Name}' failed.", step.Order, i == template.Steps.Count, logs: result.logs);
                else
                    logs.AddRange(result.logs);
                logs.Add($"Step {step.Order} - '{step.Name}' finished");

                await LogStep(execution.Id, step.Order, i == template.Steps.Count, logs);
                i++;
            }
        }

        private async Task LogStep(long executionId, int stepId, bool isLastStep = false, List<string> logs = null)
        {
            var request = new RestRequest($"api/exec/{executionId}", Method.PUT);

            if (isLastStep)
                request.AddParameter("result", Execution.ExecutionState.Successed, ParameterType.QueryString);

            request.AddJsonBody(new Execution.StepState
            {
                Executor = Environment.MachineName,
                StepId = stepId,
                IsSuccessed = true,
                Log = logs.ToArray()
            });

            await this.restClient.ExecuteAsync(request, Method.PUT, CancellationToken.None);

            this.logger.LogInformation($"ExecutionId: {executionId};\nStepId: {stepId}");
            this.logger.LogInformation(string.Join('\n', logs));
        }

        private async Task LogError(long executionId, string message, int stepId = 0, bool isLastStep = false, Exception error = null, string[] logs = null)
        {
            var request = new RestRequest($"api/exec/{executionId}", Method.PUT);

            if (isLastStep)
                request.AddParameter("result", Execution.ExecutionState.Failed, ParameterType.QueryString);
            logs = (logs ?? new string[0]).Concat(new[] { message }).ToArray();
            if (error != null)
                logs = logs.Concat(new[] { error.Message, error.StackTrace }).ToArray();

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

        private async Task<Execution> GetExecution(long executionId, CancellationToken cancellationToken)
        {
            var request = new RestRequest($"api/exec/{executionId}");
            var response = await this.restClient.ExecuteAsync<Execution>(request, cancellationToken: cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data;

            this.logger.LogWarning($"Getting execution. Response status code: {response.StatusCode}.\n" +
                $"Response: {response.Content}");
            return default;
        }

        private async Task<Template> GetTemplate(long templateId, CancellationToken cancellationToken)
        {
            var request = new RestRequest($"api/templates/{templateId}");
            var response = await this.restClient.ExecuteAsync<Template>(request, cancellationToken: cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data;

            this.logger.LogWarning($"Getting template. Response status code: {response.StatusCode}.\n" +
                $"Response: {response.Content}");
            return default;
        }

        private string Substitute(string command, Dictionary<string, string> variables)
        {
            var result = command;
            foreach (var variable in variables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value, StringComparison.InvariantCultureIgnoreCase);
            }
            return result;
        }
    }
}
