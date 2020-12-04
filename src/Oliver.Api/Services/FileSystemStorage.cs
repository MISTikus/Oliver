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

        public async Task Save(string id, IFormFile formFile)
        {
            if (!Directory.Exists(this.storageFolder))
                Directory.CreateDirectory(this.storageFolder);

            var fileName = Path.Combine(this.storageFolder, id);

            //using var fileStream = File.Create(fileName);
            //stream.Seek(0, SeekOrigin.Begin);
            //stream.CopyTo(fileStream);
            //stream.Close();

            using var fileStream = new FileStream(fileName, FileMode.Create);
            await formFile.CopyToAsync(fileStream); // ToDo: figure out why it creates an empty file
        }

        public void Dispose() { }
    }

    public interface IBlobStorage : IDisposable
    {
        Task Save(string id, IFormFile formFile);
    }
}
