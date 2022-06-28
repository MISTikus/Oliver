using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Oliver.Common.Models;

namespace Oliver.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1")]
public class TemplatesController : ControllerBase
{
    private readonly Func<ILiteDatabase> databaseFactory;

    public TemplatesController(Func<ILiteDatabase> databaseFactory) => this.databaseFactory = databaseFactory;

    [HttpGet("{id}")]
    public ActionResult<Template> GetTemplate([FromRoute] long id)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<Template> collection = db.GetCollection<Template>();
        Template template = collection.FindById(id);
        return template is null
            ? NotFound()
            : Ok(template);
    }

    [HttpPost]
    public ActionResult<long> AddTemplate([FromBody] Template template)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<Template> collection = db.GetCollection<Template>();
        collection.Insert(template);
        return Ok(template.Id);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateTemplate([FromQuery] long id, [FromBody] Template template)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<Template> collection = db.GetCollection<Template>();
        Template existing = collection.FindById(id);
        if (existing is null)
            return NotFound();
        collection.Update(template);
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult RemoveTemplate([FromQuery] long id)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<Template> collection = db.GetCollection<Template>();
        Template existing = collection.FindById(id);
        if (existing is null)
            return NotFound();
        collection.Delete(id);
        return Ok();
    }
}
