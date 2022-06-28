using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oliver.Api.Services;
using Oliver.Common.Extensions;
using Oliver.Common.Models.ApiContracts;
using File = Oliver.Common.Models.File;

namespace Oliver.Api.Controllers;

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
        logger.LogInformation($"Received: {request.Version}. {request.Body?.FileName}:{request.Body?.Length}");
        logger.LogTrace("Request.Body: \n" + await Request.Body.ReadAsStringAsync("NULL", true));
        logger.LogTrace("Request.Form: \n" + JsonConvert.SerializeObject(Request.Form));
        logger.LogTrace("Request.FormFiles: \n" + JsonConvert.SerializeObject(Request.Form?.Files));
        logger.LogTrace("Request.Headers: \n" + JsonConvert.SerializeObject(Request.Headers));
        logger.LogTrace("Request.Method: \n" + JsonConvert.SerializeObject(Request.Method));
        logger.LogTrace("Request.Path: \n" + JsonConvert.SerializeObject(Request.Path));
        logger.LogTrace("Request.Query: \n" + JsonConvert.SerializeObject(Request.Query));
        logger.LogTrace("Request.RouteValues: \n" + JsonConvert.SerializeObject(Request.RouteValues));

        if (request is null || request.Body is null)
            return BadRequest("Empty request or body");

        IFormFile formFile = request.Body;

        using ILiteDatabase db = databaseFactory();
        File file = MapToFile(request);
        ILiteCollection<File> collection = db.GetCollection<File>();

        File existing = collection.FindById(file.Id);
        if (existing is null)
            collection.Insert(file);
        else
            collection.Update(file);

        using IBlobStorage storage = storageFactory();
        await storage.SaveAsync(file.FileName, file.Version, formFile);

        return Ok(file.Id);
    }

    [HttpGet("{fileName}")]
    public async Task<ActionResult<File>> GetFile([FromRoute] string fileName, [FromQuery] string version)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest();

        using ILiteDatabase db = databaseFactory();
        ILiteCollection<File> collection = db.GetCollection<File>();

        File file = null;
        if (string.IsNullOrWhiteSpace(version))
        {
            File latest = collection.Query()
                .Where(f => f.FileName == fileName)
                .OrderBy(f => f.Version, Query.Descending)
                .FirstOrDefault();
            if (latest is { })
                file = latest;
        }
        else
        {
            file = collection.FindById($"{fileName}:{version}");
        }

        if (file is null)
            return NotFound();

        using IBlobStorage storage = storageFactory();
        file = file with { Body = new List<byte>(await storage.ReadAsync(file.FileName, file.Version)) };

        return file.Body is null
            ? NotFound()
            : Ok(file);
    }

    private static File MapToFile(FileRequest request)
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
}
