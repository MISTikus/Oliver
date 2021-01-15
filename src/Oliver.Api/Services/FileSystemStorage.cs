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

        public async Task Save(string fileName, string version, IFormFile formFile)
        {
            var folder = Path.Combine(this.storageFolder, fileName.Replace(".", "_"), version);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, fileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await formFile.CopyToAsync(fileStream);
        }

        public Task<byte[]> Read(string fileName, string version)
        {
            var folder = fileName.Replace(".", "_");
            var filePath = Path.Combine(this.storageFolder, folder, version, fileName);
            return File.Exists(filePath)
                ? File.ReadAllBytesAsync(filePath)
                : null;
        }

        public void Dispose() { }

    }

    public interface IBlobStorage : IDisposable
    {
        Task Save(string fileName, string version, IFormFile formFile);
        Task<byte[]> Read(string fileName, string version);
    }
}
