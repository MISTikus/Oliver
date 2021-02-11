using LiteDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oliver.Common.Models;
using System;

namespace Oliver.Api.Controllers
{
    [ApiController]
    [Route("api/templates")]
    public class TemplatesController : ControllerBase
    {
        private readonly Func<ILiteDatabase> databaseFactory;

        public TemplatesController(Func<ILiteDatabase> databaseFactory) => this.databaseFactory = databaseFactory;

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Template))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetTemplate([FromRoute] long id)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            var template = collection.FindById(id);
            return template is null
                ? (IActionResult)NotFound()
                : Ok(template);
        }

        [HttpPost]
        public ActionResult<long> AddTemplate([FromBody] Template template)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            collection.Insert(template);
            return Ok(template.Id);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateTemplate([FromQuery] long id, [FromBody] Template template)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            var existing = collection.FindById(id);
            if (existing is null)
                return NotFound();
            collection.Update(template);
            return Ok();
        }

        [HttpDelete("{id}")]
        public IActionResult RemoveTemplate([FromQuery] long id)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            var existing = collection.FindById(id);
            if (existing is null)
                return NotFound();
            collection.Delete(id);
            return Ok();
        }
    }
}
