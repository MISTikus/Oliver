using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oliver.IntegrationTests.Helpers;

public abstract class ClientServerTestBase : IDisposable
{
    // To detect redundant calls
    private bool disposed = false;

#if DEBUG
    protected const string buildConfiguration = "Debug";
#else
    protected const string buildConfiguration = "Release";
#endif

    private readonly List<Process> processes = new();
    private Task[] loggingTasks;

    protected readonly CancellationTokenSource cancellation;
    protected readonly JsonSerializerOptions jsonOptions;

    protected readonly ConcurrentDictionary<string, List<string>> Logs = new();
    protected static readonly string solutionFolder = Path.GetFullPath(@"..\..\..\..\..");

    protected ClientServerTestBase()
    {
        cancellation = new CancellationTokenSource();

        jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.PropertyNameCaseInsensitive = true;
    }

    protected void StartProcesses(params (string name, ProcessStartInfo info)[] processInfos)
    {
        List<Task> loggingTaskList = new();
        foreach ((var name, ProcessStartInfo info) in processInfos)
        {
            Logs.AddOrUpdate(name, new List<string>(), (k, l) => l);
            Process process = null;
            loggingTaskList.Add(Task.Run(async () => await ReadOutputAsync(name, process), cancellation.Token));
            loggingTaskList.Add(Task.Run(async () => await ReadErrorAsync(name, process), cancellation.Token));
            process = Process.Start(info);
            processes.Add(process);
        }
        loggingTasks = loggingTaskList.ToArray();
    }

    private async Task ReadOutputAsync(string name, Process process)
    {
        while (!cancellation.IsCancellationRequested)
        {
            if (process is not null)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    Logs[name].Add(line);
            }
            await Task.Delay(100);
        }
    }
    private async Task ReadErrorAsync(string name, Process process)
    {
        while (!cancellation.IsCancellationRequested)
        {
            if (process is not null)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    Logs[name].Add(line);
            }
            await Task.Delay(100);
        }
    }

    protected async Task<T> RetryWhileCheckIsFalseAndTimeoutNotExpiredAsync<T>(
        Func<Task<T>> getter, Func<T, bool> checker, TimeSpan timeout)
    {
        Stopwatch sw = new();
        sw.Start();

        while (!cancellation.IsCancellationRequested && sw.Elapsed <= timeout)
        {
            T model = await getter();
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
        if (disposed)
            return;

        if (disposing)
        {
            // dispose managed state (managed objects).
            cancellation.Cancel();
        }

        // free unmanaged resources (unmanaged objects) and override a finalizer below.
        // set large fields to null.
        KillApps();

        disposed = true;
    }

    private void KillApps()
    {
        foreach (Process process in processes)
            process.Kill();
    }

    #endregion Disposing
}
