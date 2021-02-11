using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Oliver.Api.Services
{
    public class FileSystemStorage : IBlobStorage
    {
        private readonly string storageFolder;

        public FileSystemStorage(string storageFolder) => this.storageFolder = storageFolder;

        public async Task SaveAsync(string fileName, string version, IFormFile formFile)
        {
            var folder = Path.Combine(this.storageFolder, FormatFileName(fileName), version);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, fileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await formFile.CopyToAsync(fileStream);
        }

        public async Task<byte[]> ReadAsync(string fileName, string version)
        {
            var folder = FormatFileName(fileName);
            var filePath = Path.Combine(this.storageFolder, folder, version, fileName);
            return File.Exists(filePath)
                ? await File.ReadAllBytesAsync(filePath)
                : null;
        }

        private static string FormatFileName(string fileName) => fileName.Replace(".", "_");

        public void Dispose() { }

    }

    public interface IBlobStorage : IDisposable
    {
        Task SaveAsync(string fileName, string version, IFormFile formFile);
        Task<byte[]> ReadAsync(string fileName, string version);
    }
}
