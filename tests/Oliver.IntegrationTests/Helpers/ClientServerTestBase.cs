using Oliver.Client.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.IntegrationTests.Helpers
{
    public class ClientServerTestBase : IDisposable
    {
        // To detect redundant calls
        private bool disposed = false;

#if DEBUG
        private const string buildConfiguration = "Debug";
#else
        private const string buildConfiguration = "Release";
#endif
        protected const string apiHost = "https://localhost:5001/";
        protected const string version = "1";
        protected const string tenant = "Some";
        protected const string environment = "Prod";
        protected const string templatesApi = "api/templates";
        protected const string packagesApi = "api/packages";

        protected const string scriptArchiveFileName = "somescript.zip";
        protected const string scriptFileName = "somescript.ps1";

        protected static readonly string solutionFolder = Path.GetFullPath(@"..\..\..\..\..");

        protected readonly JsonSerializerOptions jsonOptions;
        protected readonly List<string> errors;
        protected readonly IApiClient api;

        protected static readonly List<string> ClientLog = new List<string>();
        protected static readonly List<string> ServerLog = new List<string>();

        private readonly CancellationTokenSource cancellation;
        private static Process serverProcess; // ToDo: use singleton access
        private static Process clientProcess;
        private static Task[] loggingTasks;

        protected ClientServerTestBase()
        {
            this.cancellation = new CancellationTokenSource();

            this.jsonOptions = new JsonSerializerOptions();
            this.jsonOptions.Converters.Add(new JsonStringEnumConverter());
            this.jsonOptions.IgnoreNullValues = true;
            this.jsonOptions.PropertyNameCaseInsensitive = true;

            this.errors = new List<string>();
            this.api = new OliverApiClient(apiHost, new ApiUrlHelper(version), this.jsonOptions, this.errors.Add);

            var serverFolder = Path.Combine(solutionFolder, @"src\Oliver.Api\bin", buildConfiguration);
            var clientFolder = Path.Combine(solutionFolder, @"src\Oliver.Client\bin", buildConfiguration);

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

            loggingTasks = new[]
            {
                Task.Run(ReadClientOutputAsync, this.cancellation.Token),
                Task.Run(ReadClientErrorAsync, this.cancellation.Token),
                Task.Run(ReadServerOutputAsync, this.cancellation.Token),
                Task.Run(ReadServerErrorAsync, this.cancellation.Token),
            };

            serverProcess = Process.Start(serverPSI);
            clientProcess = Process.Start(clientPSI);
        }
        protected static Exception AssertionException(string message, string errorMessage)
            => throw new ArgumentException(string.Join("\n", message, errorMessage));

        private async Task ReadClientOutputAsync()
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                if (clientProcess is not null)
                {
                    var line = await clientProcess.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        ClientLog.Add(line);
                }
            }
        }
        private async Task ReadClientErrorAsync()
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                if (clientProcess is not null)
                {
                    var line = await clientProcess.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        ClientLog.Add(line);
                }
            }
        }

        private async Task ReadServerOutputAsync()
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                if (serverProcess is not null)
                {
                    var line = await serverProcess.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        ServerLog.Add(line);
                }
            }
        }
        private async Task ReadServerErrorAsync()
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                if (serverProcess is not null)
                {
                    var line = await serverProcess.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        ServerLog.Add(line);
                }
            }
        }


        #region Disposing
        ~ClientServerTestBase() => Dispose(false);

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                // dispose managed state (managed objects).
                this.cancellation.Cancel();
            }

            // free unmanaged resources (unmanaged objects) and override a finalizer below.
            // set large fields to null.
            KillApps();

            this.disposed = true;
        }

        private static void KillApps()
        {
            clientProcess.Kill();
            serverProcess.Kill();
        }
        #endregion Disposing
    }
}
