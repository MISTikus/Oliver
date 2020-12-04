using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Oliver.Common.Models;
using System;
using System.Threading.Tasks;

namespace Oliver.Api.Controllers
{
    [ApiController]
    [Route("api/templates")]
    public class TemplatesController : ControllerBase
    {
        private readonly Func<ILiteDatabase> databaseFactory;

        public TemplatesController(Func<ILiteDatabase> databaseFactory) => this.databaseFactory = databaseFactory;

        [HttpGet("{id}")]
        public async Task<ActionResult<Template>> GetTemplate([FromRoute] long id)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            var template = collection.FindById(id);
            return template is null
                ? NotFound() as ActionResult
                : Ok(template);
        }

        [HttpPost]
        public async Task<ActionResult<long>> AddTemplate([FromBody] Template template)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            collection.Insert(template);
            return Ok(template.Id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate([FromQuery] long id, [FromBody] Template template)
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
        public async Task<IActionResult> RemoveTemplate([FromQuery] long id)
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
