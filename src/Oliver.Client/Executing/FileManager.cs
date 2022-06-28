using System.IO.Compression;

namespace Oliver.Client.Executing;

public class FileManager : IFileManager
{
    public (bool isSuccessed, string[] logs) UnpackArchive(string folder, Common.Models.File file)
    {
        List<string> logs = new()
        {
            $"Start unpacking file {file.FileName} to foler: {folder}."
        };

        try
        {
            using var stream = new MemoryStream(file.Body.ToArray());
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(folder, true);

            logs.Add($"{file.FileName} unpacked.");

            return (true, logs.ToArray());
        }
        catch (Exception e)
        {
            logs.Add($"Failed to unpack {file.FileName} because of error:");
            logs.Add(e.Message);
            logs.Add(e.StackTrace);
            // ToDo: add inner exceptions to logs
            return (false, logs.ToArray());
        }
    }
}

public interface IFileManager
{
    (bool isSuccessed, string[] logs) UnpackArchive(string folder, Common.Models.File file);
}
