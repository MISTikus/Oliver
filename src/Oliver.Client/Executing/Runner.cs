using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    internal class Runner : IRunner
    {
        public Task<(bool isSuccessed, string[] logs)> RunCMD(string folder, string command) => throw new NotImplementedException();
        public Task<(bool isSuccessed, string[] logs)> RunCompose(string folder, string command) => throw new NotImplementedException();
        public Task<(bool isSuccessed, string[] logs)> RunDocker(string folder, string command) => throw new NotImplementedException();
        public async Task<(bool isSuccessed, string[] logs)> RunPowerShell(string folder, string command)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell")
                {
                    WorkingDirectory = folder,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };
                psi.Arguments = $"-Command {command}";
                var process = Process.Start(psi);
                process.WaitForExit();
                return (process.ExitCode == 0, new[] { await process.StandardOutput.ReadToEndAsync(), await process.StandardError.ReadToEndAsync() });
            }
            catch (Exception e)
            {
                return (false, new[] { e.Message, e.StackTrace });
            }
        }
    }

    internal interface IRunner
    {
        Task<(bool isSuccessed, string[] logs)> RunPowerShell(string folder, string command);
        Task<(bool isSuccessed, string[] logs)> RunCMD(string folder, string command);
        Task<(bool isSuccessed, string[] logs)> RunDocker(string folder, string command);
        Task<(bool isSuccessed, string[] logs)> RunCompose(string folder, string command);
    }
}
