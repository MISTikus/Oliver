using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oliver.Api.Services;
using Oliver.Common.Models;
using System;
using System.Threading.Tasks;

namespace Oliver.Api.Controllers
{
    [ApiController]
    [Route("api/packages")]
    public class PackagesController : ControllerBase
    {
        private readonly Func<ILiteDatabase> databaseFactory;
        private readonly Func<IBlobStorage> storageFactory;

        public PackagesController(Func<ILiteDatabase> databaseFactory, Func<IBlobStorage> storageFactory)
        {
            this.databaseFactory = databaseFactory;
            this.storageFactory = storageFactory;
        }

        [HttpPost]
        public async Task<IActionResult> AddFile([FromForm] FileRequest request)
        {
            if (request is null || request.Body is null)
                return BadRequest();

            var formFile = request.Body;

            using var db = this.databaseFactory();
            var file = MapToFile(request);
            var collection = db.GetCollection<File>();

            var existing = collection.FindById(file.Id);
            if (existing is null)
                collection.Insert(file);
            else
                collection.Update(file);

            using var storage = this.storageFactory();
            await storage.Save(file.Id, formFile);

            return Ok(file.Id);
        }

        private File MapToFile(FileRequest request)
        {
            return new File
            {
                Id = $"{request.Body.FileName}:{request.Version}",
                FileName = request.Body.FileName,
                ContentType = request.Body.ContentType,
                Version = Version.Parse(request.Version),
                Body = null
            };
        }

        public class FileRequest
        {
            public string Version { get; set; }
            public IFormFile Body { get; set; }
        }
    }
}
