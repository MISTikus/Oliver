using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.IntegrationTests.Helpers
{
    public abstract class ClientServerTestBase : IDisposable
    {
        // To detect redundant calls
        private bool disposed = false;

#if DEBUG
        protected const string buildConfiguration = "Debug";
#else
        protected const string buildConfiguration = "Release";
#endif

        private readonly List<Process> processes = new List<Process>();
        private Task[] loggingTasks;

        protected readonly CancellationTokenSource cancellation;
        protected readonly JsonSerializerOptions jsonOptions;

        protected readonly ConcurrentDictionary<string, List<string>> Logs = new ConcurrentDictionary<string, List<string>>();
        protected static readonly string solutionFolder = Path.GetFullPath(@"..\..\..\..\..");

        protected ClientServerTestBase()
        {
            this.cancellation = new CancellationTokenSource();

            this.jsonOptions = new JsonSerializerOptions();
            this.jsonOptions.Converters.Add(new JsonStringEnumConverter());
            this.jsonOptions.IgnoreNullValues = true;
            this.jsonOptions.PropertyNameCaseInsensitive = true;
        }

        protected void StartProcesses(params (string name, ProcessStartInfo info)[] processInfos)
        {
            var loggingTaskList = new List<Task>();
            foreach (var (name, info) in processInfos)
            {
                this.Logs.AddOrUpdate(name, new List<string>(), (k, l) => l);
                Process process = null;
                loggingTaskList.Add(Task.Run(async () => await ReadOutputAsync(name, process), this.cancellation.Token));
                loggingTaskList.Add(Task.Run(async () => await ReadErrorAsync(name, process), this.cancellation.Token));
                process = Process.Start(info);
                this.processes.Add(process);
            }
            this.loggingTasks = loggingTaskList.ToArray();
        }

        private async Task ReadOutputAsync(string name, Process process)
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                if (process is not null)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        this.Logs[name].Add(line);
                }
                await Task.Delay(100);
            }
        }
        private async Task ReadErrorAsync(string name, Process process)
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                if (process is not null)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        this.Logs[name].Add(line);
                }
                await Task.Delay(100);
            }
        }

        protected async Task<T> RetryWhileCheckIsFalseAndTimeoutNotExpiredAsync<T>(
            Func<Task<T>> getter, Func<T, bool> checker, TimeSpan timeout)
        {
            var sw = new Stopwatch();
            sw.Start();

            while (!this.cancellation.IsCancellationRequested
                && sw.Elapsed <= timeout)
            {
                var model = await getter();
                if (checker(model))
                    return model;
            }
            return default;
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

        private void KillApps()
        {
            foreach (var process in this.processes)
                process.Kill();
        }
        #endregion Disposing
    }
}
