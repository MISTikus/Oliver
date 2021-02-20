using Oliver.Client.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        protected const string templatesApi = "api/templates";
        protected const string packagesApi = "api/packages";

        protected const string scriptArchiveFileName = "somescript.zip";
        protected const string scriptFileName = "somescript.cmd";

        protected static readonly string solutionFolder = Path.GetFullPath(@"..\..\..\..\..\");

        protected static Process serverProcess;
        protected static Process clientProcess;

        protected readonly JsonSerializerOptions jsonOptions;
        protected readonly List<string> errors;
        protected readonly OliverApiClient api;
        protected readonly List<string> clientLog = new List<string>();
        protected readonly List<string> serverLog = new List<string>();

        protected ClientServerTestBase()
        {
            this.jsonOptions = new JsonSerializerOptions();
            this.jsonOptions.Converters.Add(new JsonStringEnumConverter());
            this.jsonOptions.IgnoreNullValues = true;
            this.jsonOptions.PropertyNameCaseInsensitive = true;

            this.errors = new List<string>();
            this.api = new OliverApiClient(apiHost, new ApiUrlHelper(version), this.jsonOptions, this.errors.Add);

            var serverPSI = new ProcessStartInfo(Path.Combine(solutionFolder, @"src\Oliver.Api\bin", buildConfiguration, "Oliver.Api.exe"));
            var clientPSI = new ProcessStartInfo(Path.Combine(solutionFolder, @"src\Oliver.Client\bin", buildConfiguration, "Oliver.Client.exe"));
            serverProcess = Process.Start(serverPSI);
            clientProcess = Process.Start(clientPSI);

            serverProcess.ErrorDataReceived += (s, a) => this.serverLog.Add(a.Data);
            serverProcess.OutputDataReceived += (s, a) => this.serverLog.Add(a.Data);
            clientProcess.ErrorDataReceived += (s, a) => this.clientLog.Add(a.Data);
            clientProcess.OutputDataReceived += (s, a) => this.clientLog.Add(a.Data);
        }

        protected static Exception AssertionException(string message, string errorMessage)
            => throw new ArgumentException(string.Join("\n", message, errorMessage));


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
