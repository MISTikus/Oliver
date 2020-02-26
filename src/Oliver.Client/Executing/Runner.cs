using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    internal class Runner : IRunner
    {
        public async Task<(bool isSuccessed, string[] logs)> RunCMD(string folder, string command) => await Run("CMD", "/C", folder, command);
        public async Task<(bool isSuccessed, string[] logs)> RunCompose(string folder, string command) => await Run("docker-compose", "-Command", folder, command);
        public async Task<(bool isSuccessed, string[] logs)> RunDocker(string folder, string command) => await Run("docker", "", folder, command);
        public async Task<(bool isSuccessed, string[] logs)> RunPowerShell(string folder, string command) => await Run("powershell", "-Command", folder, command);

        private async Task<(bool isSuccessed, string[] logs)> Run(string exec, string argPrefix, string folder, string command)
        {
            try
            {
                var psi = new ProcessStartInfo(exec)
                {
                    WorkingDirectory = folder,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };
                psi.Arguments = $"{argPrefix} {command}";
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
