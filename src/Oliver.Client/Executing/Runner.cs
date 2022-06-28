using System.Diagnostics;

namespace Oliver.Client.Executing;

internal class Runner : IRunner
{
    public async Task<(bool isSuccessed, string[] logs)> RunCMDAsync(string folder, string command) => await RunAsync("CMD", "/C", folder, command);
    public async Task<(bool isSuccessed, string[] logs)> RunComposeAsync(string folder, string command) => await RunAsync("docker-compose", "", folder, command);
    public async Task<(bool isSuccessed, string[] logs)> RunDockerAsync(string folder, string command) => await RunAsync("docker", "", folder, command);
    public async Task<(bool isSuccessed, string[] logs)> RunPowerShellAsync(string folder, string command) => await RunAsync("powershell", "-Command", folder, command);

    private static async Task<(bool isSuccessed, string[] logs)> RunAsync(string exec, string argPrefix, string folder, string command)
    {
        List<string> logs = new();
        try
        {
            ProcessStartInfo psi = new(exec)
            {
                WorkingDirectory = folder,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                Arguments = $"{argPrefix} {command}"
            };
            logs.Add($"Executiong command:\n{psi.FileName} {psi.Arguments}");

            var process = Process.Start(psi);
            process.WaitForExit();

            string o, e;
            if (!string.IsNullOrWhiteSpace(o = await process.StandardOutput.ReadToEndAsync()))
                logs.Add(o);
            if (!string.IsNullOrWhiteSpace(e = await process.StandardError.ReadToEndAsync()))
                logs.Add(e);

            return (process.ExitCode == 0, logs.ToArray());
        }
        catch (Exception e)
        {
            logs.Add(e.Message);
            logs.Add(e.StackTrace);
            return (false, logs.ToArray());
        }
    }
}

internal interface IRunner
{
    Task<(bool isSuccessed, string[] logs)> RunPowerShellAsync(string folder, string command);
    Task<(bool isSuccessed, string[] logs)> RunCMDAsync(string folder, string command);
    Task<(bool isSuccessed, string[] logs)> RunDockerAsync(string folder, string command);
    Task<(bool isSuccessed, string[] logs)> RunComposeAsync(string folder, string command);
}
