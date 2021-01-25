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
        public Task<ActionResult> GetTemplate([FromRoute] long id)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            var template = collection.FindById(id);
            return template is null
                ? Task.FromResult<ActionResult>(NotFound())
                : Task.FromResult<ActionResult>(Ok(template));
        }

        [HttpPost]
        public Task<ActionResult<long>> AddTemplate([FromBody] Template template)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            collection.Insert(template);
            return Task.FromResult<ActionResult<long>>(Ok(template.Id));
        }

        [HttpPut("{id}")]
        public Task<IActionResult> UpdateTemplate([FromQuery] long id, [FromBody] Template template)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            var existing = collection.FindById(id);
            if (existing is null)
                return Task.FromResult<IActionResult>(NotFound());
            collection.Update(template);
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> RemoveTemplate([FromQuery] long id)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Template>();
            var existing = collection.FindById(id);
            if (existing is null)
                return Task.FromResult<IActionResult>(NotFound());
            collection.Delete(id);
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
