using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Oliver.Api.Services;
using Oliver.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Oliver.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    public class PackagesController : ControllerBase
    {
        private readonly Func<ILiteDatabase> databaseFactory;
        private readonly Func<IBlobStorage> storageFactory;
        private readonly ILogger<PackagesController> logger;

        public PackagesController(Func<ILiteDatabase> databaseFactory,
            Func<IBlobStorage> storageFactory,
            ILogger<PackagesController> logger)
        {
            this.databaseFactory = databaseFactory;
            this.storageFactory = storageFactory;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<long>> AddFile([FromForm] FileRequest request)
        {
            this.logger.Log(LogLevel.Information, new EventId(), request, null, (s, e) => $"Received: {s.Version}. {s.Body.FileName}:{s.Body.Length}");
            if (request is null || request.Body is null)
                return BadRequest("Empty request or body");

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
            await storage.SaveAsync(file.FileName, file.Version, formFile);

            return Ok(file.Id);
        }

        [HttpGet("{fileName}")]
        public async Task<ActionResult<File>> GetFile([FromRoute] string fileName, [FromQuery] string version)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest();

            using var db = this.databaseFactory();
            var collection = db.GetCollection<File>();

            File file = null;
            if (string.IsNullOrWhiteSpace(version))
            {
                var latest = collection.Query()
                    .Where(f => f.FileName == fileName)
                    .OrderBy(f => f.Version, Query.Descending)
                    .FirstOrDefault();
                if (latest is { })
                    file = latest;
            }
            else
                file = collection.FindById($"{fileName}:{version}");

            if (file is null)
                return NotFound();

            using var storage = this.storageFactory();
            file.Body = new List<byte>(await storage.ReadAsync(file.FileName, file.Version));

            return file.Body is null
                ? NotFound()
                : Ok(file);
        }

        private File MapToFile(FileRequest request)
        {
            return new File
            {
                Id = $"{request.Body.FileName}:{request.Version}",
                FileName = request.Body.FileName,
                ContentType = request.Body.ContentType,
                Version = request.Version,
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
