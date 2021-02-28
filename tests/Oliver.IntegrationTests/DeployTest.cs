using FluentAssertions;
using Oliver.Client.Services;
using Oliver.Common.Models;
using Oliver.IntegrationTests.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Oliver.IntegrationTests
{
    [Collection("Integration")]
    public class DeployTest : ClientServerTestBase
    {
        protected const string apiHost = "https://localhost:5001/";
        protected const string version = "1";
        protected const string tenant = "Some";
        protected const string environment = "Prod";

        protected const string scriptArchiveFileName = "somescript.zip";
        protected const string scriptFileName = "somescript.ps1";

        private static readonly string serverFolder = Path.Combine(solutionFolder, @"src\Oliver.Api\bin", buildConfiguration);
        private static readonly string clientFolder = Path.Combine(solutionFolder, @"src\Oliver.Client\bin", buildConfiguration);
        private readonly List<string> apiErrors;
        private readonly IApiClient api;

        public DeployTest()
        {
            var serverPSI = new ProcessStartInfo(Path.Combine(serverFolder, "Oliver.Api.exe"), "?nologs")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.Combine(solutionFolder, @"src\Oliver.Api")
            };

            var clientPSI = new ProcessStartInfo(Path.Combine(clientFolder, "Oliver.Client.exe"), "?nologs")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = clientFolder
            };
            StartProcesses(("server", serverPSI), ("client", clientPSI));

            this.apiErrors = new List<string>();
            this.api = new OliverApiClient(apiHost, new ApiUrlHelper(version), this.jsonOptions, this.apiErrors.Add);
        }

        [Fact]
        public async Task Deploy_Should_Execute_Properly()
        {
            // Arrange
            var packageId = await CreatePackageAsync();
            var templateId = await CreateTemplateAsync();
            var variableSetId = await CreateVariableSetAsync();

            // Action
            var executionId = await CreateExecutionAsync(templateId, variableSetId);
            var execution = await RetryWhileCheckIsFalseAndTimeoutNotExpiredAsync(
                () => this.api.GetExecutionAsync(executionId),
                ex => ex?.State != Execution.ExecutionState.Added,
                TimeSpan.FromSeconds(5));

            // Assert
            execution.Should().NotBeNull();

            execution.State.Should().Be(Execution.ExecutionState.Successed, "\n" + string.Join('\n',
                execution.StepsStates
                    .Select(s => $"{s.StepId}. {s.StepName}.{(s.IsSuccess ? "S" : "F")}: " +
                        $"{string.Join("\n\t", s.Log)}")
                    .Concat(new[] { "Client log:" }.Concat(this.Logs["client"]))));

            var extractArchiveLog = execution.StepsStates.First(x => x.StepId == 1).Log;
            extractArchiveLog
                .Should()
                .Contain(@$"Executing at folder: '{solutionFolder}\artifacts\workFolder\{tenant}'");

            var echoLog = execution.StepsStates.First(x => x.StepId == 2).Log;
            echoLog
                .Should()
                .Contain("Johny, It's Allive!!!");

            var variableStepLog = execution.StepsStates.First(x => x.StepId == 3).Log;
            variableStepLog
                .Should()
                .Contain($"{tenant}{environment}TemplateVariableValueExecutionVariableValue");
        }

        private async Task<string> CreatePackageAsync()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            var scriptTempFileName = Path.Combine(tempFolder, scriptFileName);
            await System.IO.File.WriteAllTextAsync(scriptTempFileName, "Write-Host \"{Caller}, It's Allive!!!\";");
            var scriptArchiveTempFileName = Path.Combine(Path.GetTempPath(), scriptArchiveFileName);
            ZipFile.CreateFromDirectory(tempFolder, scriptArchiveTempFileName);

            var id = await this.api.CreatePackageAsync(scriptArchiveTempFileName, "1.0.1");
            this.apiErrors.Should().BeEmpty(string.Join('\n', new[] { "Server log:" }.Concat(this.Logs["server"])));

            Directory.Delete(tempFolder, true);
            if (System.IO.File.Exists(scriptArchiveTempFileName))
                System.IO.File.Delete(scriptArchiveTempFileName);
            return id;
        }

        private async Task<long> CreateTemplateAsync()
        {
            var template = new Template
            {
                Steps = new List<Template.Step>
                {
                    new Template.Step
                    {
                        Order = 1,
                        Name = "Extract archive",
                        Type = Template.StepType.Archive,
                        FileName = scriptArchiveFileName,
                        WorkingFolder = @".\{Tenant}"
                    },
                    new Template.Step
                    {
                        Order = 2,
                        Name = "Run script from archive",
                        Type = Template.StepType.PShell,
                        Command = $@".\{{Tenant}}\{scriptFileName}"
                    },
                    new Template.Step
                    {
                        Order = 3,
                        Name = "Variables",
                        Type = Template.StepType.CMD,
                        Command = "echo {Tenant}{Environment}{TemplateVariable}{ExecutionVariable}"
                    }
                }
            };

            var id = await this.api.CreateTemplateAsync(template);
            this.apiErrors.Should().BeEmpty(string.Join('\n', new[] { "Server log:" }.Concat(this.Logs["server"])));
            return id;
        }

        private async Task<long> CreateVariableSetAsync()
        {
            var variableSet = new VariableSet
            {
                Instance = new Instance(tenant, environment),
                Values = { { "TemplateVariable", "TemplateVariableValue" }, { "Caller", "Johny" } }
            };

            var id = await this.api.CreateVariableSetAsync(variableSet);
            this.apiErrors.Should().BeEmpty(string.Join('\n', new[] { "Server log:" }.Concat(this.Logs["server"])));
            return id;
        }

        private async Task<long> CreateExecutionAsync(long templateId, long variableSetId)
        {
            var execution = new Execution
            {
                Instance = new Instance(tenant, environment),
                TemplateId = templateId,
                VariableSetId = variableSetId,
                VariableOverrides = { { "ExecutionVariable", "ExecutionVariableValue" } }
            };

            var id = await this.api.CreateExecutionAsync(execution);
            this.apiErrors.Should().BeEmpty(string.Join('\n', new[] { "Server log:" }.Concat(this.Logs["server"])));
            return id;
        }
    }
}