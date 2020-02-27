using System;
using System.Collections.Generic;
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
            var logs = new List<string>();
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
        Task<(bool isSuccessed, string[] logs)> RunPowerShell(string folder, string command);
        Task<(bool isSuccessed, string[] logs)> RunCMD(string folder, string command);
        Task<(bool isSuccessed, string[] logs)> RunDocker(string folder, string command);
        Task<(bool isSuccessed, string[] logs)> RunCompose(string folder, string command);
    }
}
