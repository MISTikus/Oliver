using Oliver.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Oliver.Client.Executing
{
    public class FileManager : IFileManager
    {
        public async Task<(bool isSuccessed, string[] logs)> UnpackArchive(string folder, Template.Step step)
        {
            var logs = new List<string>
            {
                $"Start unpacking file {step.Command} to foler: {folder}."
            };

            try
            {
                using var stream = new MemoryStream(step.Body.Body);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(folder);

                logs.Add($"{step.Command} unpacked.");

                return (true, logs.ToArray());
            }
            catch (Exception e)
            {
                logs.Add($"Failed to unpack {step.Command} because of error:");
                logs.Add(e.Message);
                logs.Add(e.StackTrace);
                // ToDo: add inner exceptions to logs
                return (false, logs.ToArray());
            }
        }
    }

    public interface IFileManager
    {
        Task<(bool isSuccessed, string[] logs)> UnpackArchive(string folder, Template.Step step);
    }
}
