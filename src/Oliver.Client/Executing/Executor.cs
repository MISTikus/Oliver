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
        private readonly IOptions<Configurations.Client> instanceOptions;
        private readonly ILogger<Executor> logger;
        private readonly IRunner runner;
        private readonly IFileManager fileManager;
        private readonly ILogSender logSender;

        public Executor(IRestClient restClient, IOptions<Configurations.Client> instanceOptions,
            IRunner runner, IFileManager fileManager,
            ILogSender logSender, ILogger<Executor> logger)
        {
            this.restClient = restClient;
            this.instanceOptions = instanceOptions;
            this.logger = logger;
            this.runner = runner;
            this.fileManager = fileManager;
            this.logSender = logSender;
        }

        public async Task ExecuteAsync(Configurations.Client.Instance instance, long executionId, CancellationToken cancellationToken)
        {
            try
            {
                var execution = await GetExecutionAsync(executionId, cancellationToken).ConfigureAwait(false);
                await ExecuteAsync(instance, execution, cancellationToken);
            }
            catch (Exception e)
            {
                await this.logSender.LogError(executionId, "Failed to execute", isLastStep: true, error: e, cancellationToken: cancellationToken);
            }
        }

        private async Task ExecuteAsync(Configurations.Client.Instance instance, Execution execution, CancellationToken cancellationToken)
        {
            var template = await GetTemplateAsync(execution.TemplateId, cancellationToken);
            var variables = execution.VariableSetId == default
                ? null
                : await GetVariablesAsync(execution.VariableSetId, execution.VariableOverrides, cancellationToken);

            // Builtin variables
            variables?.Values.Add(nameof(execution.Instance.Tenant), execution.Instance.Tenant);
            variables?.Values.Add(nameof(execution.Instance.Environment), execution.Instance.Environment);

            var i = 1;
            foreach (var step in template.Steps.OrderBy(x => x.Order))
            {
                var logs = new List<string> { $"Starting step {step.Order}: '{step.Name}'" };

                var command = Substitute(step.Command, variables?.Values);

                var folder = Substitute(step.WorkingFolder, variables?.Values);
                folder = Path.GetFullPath(folder, Path.GetFullPath(this.instanceOptions?.Value?.DefaultFolder ?? "."));
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    logs.Add($"Folder: '{folder}' created.");
                }
                logs.Add($"Executing at folder: '{folder}'");

                Common.Models.File file;
                var result = step.Type switch
                {
                    Template.StepType.Archive when (file = await GetArchiveAsync(step.FileName)) is { }
                        => this.fileManager.UnpackArchive(folder, file),
                    Template.StepType.CMD => await this.runner.RunCMDAsync(folder, command),
                    Template.StepType.PShell => await this.runner.RunPowerShellAsync(folder, command),
                    Template.StepType.Docker => await this.runner.RunDockerAsync(folder, command),
                    Template.StepType.DockerCompose => await this.runner.RunComposeAsync(folder, command),
                    _ => throw new NotImplementedException(),
                };

                if (!result.isSuccessed)
                {
                    await this.logSender.LogError(execution.Id, $"Step '{step.Name}' failed.", step.Order, true, logs: result.logs.ToList(), cancellationToken: cancellationToken);
                    break;
                }
                else
                    logs.AddRange(result.logs);

                logs.Add($"Step {step.Order} - '{step.Name}' finished");

                await this.logSender.LogStep(execution.Id, step.Order, i == template.Steps.Count, logs, cancellationToken: cancellationToken);
                i++;
            }
        }

        private async Task<Execution> GetExecutionAsync(long executionId, CancellationToken cancellationToken)
        {
            // ToDo: move version to constant
            var request = new RestRequest($"api/v1/executions/{executionId}");
            var response = await this.restClient.ExecuteAsync<Execution>(request, cancellationToken: cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data;

            this.logger.LogWarning($"Getting execution. Response status code: {response.StatusCode}.\n" +
                $"Response: {response.Content}");
            return default;
        }

        private async Task<Template> GetTemplateAsync(long templateId, CancellationToken cancellationToken)
        {
            var request = new RestRequest($"api/v1/templates/{templateId}");
            var response = await this.restClient.ExecuteAsync<Template>(request, cancellationToken: cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data;

            this.logger.LogWarning($"Getting template. Response status code: {response.StatusCode}.\n" +
                $"Response: {response.Content}");
            return default;
        }

        private async Task<VariableSet> GetVariablesAsync(long variableSetId, Dictionary<string, string> overrides, CancellationToken cancellationToken)
        {
            var request = new RestRequest($"api/v1/variables/{variableSetId}");
            var response = await this.restClient.ExecuteAsync<VariableSet>(request, cancellationToken: cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = response.Data;
                foreach (var key in overrides.Keys)
                    result.Values[key] = overrides[key];
                return result;
            }

            this.logger.LogWarning($"Getting variables. Response status code: {response.StatusCode}.\n" +
                $"Response: {response.Content}");
            return default;
        }

        private async Task<Common.Models.File> GetArchiveAsync(string fileName, string version = null, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest($"api/packages/{fileName}"
                + (string.IsNullOrWhiteSpace(version) ? "" : $"?version={version}"));
            var response = await this.restClient.ExecuteAsync<Common.Models.File>(request, cancellationToken: cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data;

            this.logger.LogWarning($"Getting template. Response status code: {response.StatusCode}.\n" +
                $"Response: {response.Content}");
            return default;
        }

        private string Substitute(string command, Dictionary<string, string> variables)
        {
            if (variables is null)
                return command;

            var result = command;
            string temp;
            do
            {
                temp = result;
                foreach (var variable in variables)
                {
                    result = result.Replace($"{{{variable.Key}}}", variable.Value, StringComparison.InvariantCultureIgnoreCase);
                }
            }
            while (temp != result);
            return result;
        }
    }
}
