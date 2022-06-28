using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Oliver.Common.Models;

namespace Oliver.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1")]
public class VariablesController : ControllerBase
{
    private readonly Func<ILiteDatabase> databaseFactory;

    public VariablesController(Func<ILiteDatabase> databaseFactory) => this.databaseFactory = databaseFactory;

    [HttpGet("{tenant}/{environment}")]
    public ActionResult<VariableSet> Get([FromRoute] string tenant, [FromRoute] string environemnt)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<VariableSet> collection = db.GetCollection<VariableSet>();
        VariableSet variables = collection.FindOne(x => x.Instance.Tenant == tenant && x.Instance.Environment == environemnt);
        return variables is null
            ? NotFound()
            : Ok(variables);
    }

    [HttpGet("{id}")]
    public ActionResult<VariableSet> Get([FromRoute] long id)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<VariableSet> collection = db.GetCollection<VariableSet>();
        VariableSet variables = collection.FindById(id);
        return variables is null
            ? NotFound()
            : Ok(variables);
    }

    [HttpPost]
    public ActionResult<long> Add([FromBody] VariableSet variables)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<VariableSet> collection = db.GetCollection<VariableSet>();
        collection.Insert(variables);
        return Ok(variables.Id);
    }

    [HttpPut("{id}")]
    public IActionResult Update([FromQuery] long id, [FromBody] VariableSet variables)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<VariableSet> collection = db.GetCollection<VariableSet>();
        VariableSet existing = collection.FindById(id);
        if (existing is null)
            return NotFound();
        collection.Update(variables);
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Remove([FromQuery] long id)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<VariableSet> collection = db.GetCollection<VariableSet>();
        VariableSet existing = collection.FindById(id);
        if (existing is null)
            return NotFound();
        collection.Delete(id);
        return Ok();
    }
}
