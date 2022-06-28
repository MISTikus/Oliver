using DiskQueue;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Oliver.Api.Extensions;
using Oliver.Common.Models;

namespace Oliver.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1")]
public class ExecutionsController : ControllerBase
{
    private readonly Func<Instance, IPersistentQueue> queueFactory;
    private readonly Func<ILiteDatabase> databaseFactory;
    private readonly ILogger<ExecutionsController> logger;
    private static readonly Dictionary<Instance, IPersistentQueue> queues = new Dictionary<Instance, IPersistentQueue>();
    private static readonly object locker = new object();

    public ExecutionsController(Func<Instance, IPersistentQueue> queueFactory, Func<ILiteDatabase> databaseFactory, ILogger<ExecutionsController> logger)
    {
        this.queueFactory = queueFactory;
        this.databaseFactory = databaseFactory;
        this.logger = logger;
    }

    [HttpGet("{tenant}/{environment}/check")]
    public async Task<ActionResult<long>> CheckForExecution([FromRoute] string tenant, [FromRoute] string environment, CancellationToken cancellation)
    {
        Instance instance = new(tenant, environment);

        lock (locker)
        {
            if (!queues.ContainsKey(instance))
                queues.Add(instance, queueFactory(instance));
        }
        IPersistentQueue queue = queues[instance];
        using IPersistentQueueSession session = queue.OpenSession();

        long executionId = default;
        while ((executionId = session.Dequeue()?.Deserialize<ValueTuple<long>>().Item1 ?? default) == default
            && !cancellation.IsCancellationRequested)
        {
            await Task.Delay(100, cancellation);
        }

        if (executionId == default)
            return NotFound();

        session.Flush();

        return Ok(executionId);
    }

    [HttpGet("{id}")]
    public ActionResult<Execution> GetExecution([FromRoute] long id)
    {
        using ILiteDatabase db = databaseFactory();
        ILiteCollection<Execution> collection = db.GetCollection<Execution>();
        Execution execution = collection.FindById(id);
        return execution is null
            ? NotFound()
            : Ok(execution);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> AddExecutionStepLog([FromRoute] long id, [FromBody] Execution.StepState stepState,
        [FromQuery] Execution.ExecutionState? result)
    {
        logger.LogInformation("Received execution step log.");
        logger.LogInformation($"ExecutionId: '{id}'. Result: {result}.");
        logger.LogDebug($"Logs:");
        logger.LogDebug(string.Join('\n', stepState.Log));

        using ILiteDatabase db = databaseFactory();
        ILiteCollection<Execution> collection = db.GetCollection<Execution>();
        Execution execution = collection.FindById(id);
        execution.StepsStates.Add(stepState);

        if (result.HasValue)
        {
            if (result.Value == Execution.ExecutionState.Failed)
            {
                if (execution.State != Execution.ExecutionState.Retrying || execution.RetryCount < 3)
                {
                    await Task.Delay(3000);

                    lock (locker)
                    {
                        if (!queues.ContainsKey(execution.Instance))
                            queues.Add(execution.Instance, queueFactory(execution.Instance));
                    }
                    IPersistentQueue queue = queues[execution.Instance];
                    using IPersistentQueueSession session = queue.OpenSession();
                    session.Enqueue(new ValueTuple<long>(execution.Id).Serialize());
                    session.Flush();

                    execution.Retrying();
                }
                else
                {
                    execution.Failed();
                    logger.LogTrace($"Execution {id} failed.");
                }
            }
            else
            {
                execution.Succeeded();
                logger.LogTrace($"Execution {id} finished.");
            }
        }

        collection.Update(execution);

        return Ok();
    }

    [HttpPost]
    public ActionResult<long> AddExecution([FromBody] Execution execution)
    {
        lock (locker)
        {
            if (!queues.ContainsKey(execution.Instance))
                queues.Add(execution.Instance, queueFactory(execution.Instance));
        }
        IPersistentQueue queue = queues[execution.Instance];

        using ILiteDatabase db = databaseFactory();

        ILiteCollection<Execution> collection = db.GetCollection<Execution>();
        IEnumerable<Execution> previous = collection.Find(x => x.Instance.Tenant == execution.Instance.Tenant && x.Instance.Environment == execution.Instance.Environment);
        foreach (Execution previousExecution in previous)
        {
            if (previousExecution.State == Execution.ExecutionState.Added || previousExecution.State == Execution.ExecutionState.Retrying)
            {
                previousExecution.Declined();
                collection.Update(previousExecution);
            }
        }

        collection.Insert(execution);

        using IPersistentQueueSession session = queue.OpenSession();
        session.Enqueue(new ValueTuple<long>(execution.Id).Serialize());
        session.Flush();

        return Ok(execution.Id);
    }
}