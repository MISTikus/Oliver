using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oliver.Client.Services;
using Oliver.Common.Models;

namespace Oliver.Client.Executing;

internal class Executor : IExecutor
{
    private readonly IApiClient apiClient;
    private readonly IOptions<Configurations.Client> instanceOptions;
    private readonly ILogger<Executor> logger;
    private readonly IRunner runner;
    private readonly IFileManager fileManager;
    private readonly ILogSender logSender;

    private const string substituteFileTypes = "*.sql;*.txt;*.ps1;*.cmd;*.js;*.json";

    public Executor(IApiClient apiClient, IOptions<Configurations.Client> instanceOptions,
        IRunner runner, IFileManager fileManager,
        ILogSender logSender, ILogger<Executor> logger)
    {
        this.apiClient = apiClient;
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
            Execution execution = await apiClient.GetExecutionAsync(executionId, cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(instance, execution, cancellationToken);
        }
        catch (Exception e)
        {
            await logSender.LogError(executionId, "Failed to execute", isLastStep: true, error: e, cancellationToken: cancellationToken);
        }
    }

    private async Task ExecuteAsync(Configurations.Client.Instance instance, Execution execution, CancellationToken cancellationToken)
    {
        Template template = await apiClient.GetTemplateAsync(execution.TemplateId, cancellationToken);
        VariableSet variables = execution.VariableSetId == default
            ? new VariableSet()
            : await GetVariablesAsync(execution.VariableSetId, execution.VariableOverrides, cancellationToken);

        // Builtin variables
        variables.Values.Add(nameof(execution.Instance.Tenant), execution.Instance.Tenant);
        variables.Values.Add(nameof(execution.Instance.Environment), execution.Instance.Environment);

        var i = 1;
        foreach (Template.Step step in template.Steps.OrderBy(x => x.Order))
        {
            List<string> logs = new() { $"Starting step {step.Order}: '{step.Name}'" };

            var command = string.IsNullOrWhiteSpace(step.Command)
                ? ""
                : Substitute(step.Command, variables.Values);

            var folder = Substitute(step.WorkingFolder, variables.Values);
            folder = Path.GetFullPath(folder, Path.GetFullPath(instanceOptions?.Value?.DefaultFolder ?? "."));
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                logs.Add($"Folder: '{folder}' created.");
            }
            logs.Add($"Executing at folder: '{folder}'");
            (bool isSuccessed, string[] logs) result = step.Type switch
            {
                Template.StepType.Archive => await UnpackArchiveAsync(step.FileName, folder, variables.Values),
                Template.StepType.CMD => await runner.RunCMDAsync(folder, command),
                Template.StepType.PShell => await runner.RunPowerShellAsync(folder, command),
                Template.StepType.Docker => await runner.RunDockerAsync(folder, command),
                Template.StepType.DockerCompose => await runner.RunComposeAsync(folder, command),
                _ => throw new NotImplementedException(),
            };

            if (result.isSuccessed)
            {
                logs.AddRange(result.logs);
            }
            else
            {
                await logSender.LogError(execution.Id, $"Step '{step.Name}' failed.", step.Order, true, logs: result.logs.ToList(), cancellationToken: cancellationToken);
                break;
            }

            logs.Add($"Step {step.Order} - '{step.Name}' finished");

            await logSender.LogStep(execution.Id, step.Order, i == template.Steps.Count, logs, cancellationToken: cancellationToken);
            i++;
        }
    }

    private async Task<(bool isSuccessed, string[] logs)> UnpackArchiveAsync(string fileName, string folder, Dictionary<string, string> variables)
    {
        Common.Models.File file = await apiClient.GetPackageAsync(fileName);
        if (file is null)
            return (false, new[] { $"Failed to download archive with name ''" });

        (bool isSuccessed, string[] logs) result = fileManager.UnpackArchive(folder, file);
        if (!result.isSuccessed)
            return result;

        (bool isSuccessed, string[] logs) subResult = await SubstituteFilesInFolderAsync(folder, variables);
        var logs = result.logs.Concat(subResult.logs).ToArray();
        return (subResult.isSuccessed, logs);
    }

    private async Task<VariableSet> GetVariablesAsync(long variableSetId, Dictionary<string, string> overrides, CancellationToken cancellationToken)
    {
        VariableSet set = await apiClient.GetVariableSetAsync(variableSetId, cancellationToken);
        if (set is null)
            return default;

        foreach (var key in overrides.Keys)
            set.Values[key] = overrides[key];
        return set;
    }

    private static string Substitute(string text, Dictionary<string, string> variables)
    {
        if (variables is null)
            return text;

        var result = text;
        string temp;
        do
        {
            temp = result;
            foreach (KeyValuePair<string, string> variable in variables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value, StringComparison.InvariantCultureIgnoreCase);
            }
        }
        while (temp != result);
        return result;
    }

    private static async Task<(bool isSuccessed, string[] logs)> SubstituteFilesInFolderAsync(
        string folder, Dictionary<string, string> variables)
    {
        List<string> logs = new();
        try
        {
            var fileTypes = substituteFileTypes.Split(';');
            foreach (var filePath in fileTypes.SelectMany(x => Directory.EnumerateFiles(folder, x)))
            {
                logs.Add($"Replacing variables in file: {filePath}");
                var text = await System.IO.File.ReadAllTextAsync(filePath);
                text = Substitute(text, variables);
                await System.IO.File.WriteAllTextAsync(filePath, text);
                logs.Add($"Variables in file '{filePath}' succesfuly replaced.");
            }
            return (true, logs.ToArray());
        }
        catch (Exception)
        {
            return (false, logs.ToArray());
        }
    }
}
