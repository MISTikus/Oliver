using DiskQueue;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Oliver.Api.Extensions;
using Oliver.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Oliver.Api.Controllers
{
    [ApiController]
    [Route("api/exec")]
    public class ExecutionsController : ControllerBase
    {
        private readonly Func<Instance, IPersistentQueue> queueFactory;
        private readonly Func<ILiteDatabase> databaseFactory;

        private static readonly Dictionary<Instance, IPersistentQueue> queues = new Dictionary<Instance, IPersistentQueue>();
        private static readonly object locker = new object();

        public ExecutionsController(Func<Instance, IPersistentQueue> queueFactory, Func<ILiteDatabase> databaseFactory)
        {
            this.queueFactory = queueFactory;
            this.databaseFactory = databaseFactory;
        }

        [HttpGet("{tenant}/{environment}/check")]
        public async Task<ActionResult<long>> CheckForExecution([FromRoute]string tenant, [FromRoute]string environment, CancellationToken cancellation)
        {
            var instance = new Instance(tenant, environment);

            lock (locker)
            {
                if (!queues.ContainsKey(instance))
                    queues.Add(instance, this.queueFactory(instance));
            }
            var queue = queues[instance];
            using var session = queue.OpenSession();

            long executionId = default;
            while ((executionId = session.Dequeue()?.Deserialize<ValueTuple<long>>().Item1 ?? default) == default
                && !cancellation.IsCancellationRequested)
                await Task.Delay(100);

            if (executionId == default)
                return NotFound();

            session.Flush();

            return Ok(executionId);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Execution>> GetExecution([FromRoute]long id)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Execution>();
            var execution = collection.FindById(id);
            return execution is null
                ? NotFound() as ActionResult
                : Ok(execution);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AddExecutionStepLog([FromRoute]long id, [FromBody]Execution.StepState stepState,
            [FromQuery]Execution.ExecutionState? result)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Execution>();
            var execution = collection.FindById(id);
            execution.StepsStates.Add(stepState);

            if (result.HasValue)
                execution.State = result.Value;

            collection.Update(execution);

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<long>> AddExecution([FromBody]Execution execution)
        {
            lock (locker)
            {
                if (!queues.ContainsKey(execution.Instance))
                    queues.Add(execution.Instance, this.queueFactory(execution.Instance));
            }
            var queue = queues[execution.Instance];

            using var db = this.databaseFactory();

            var collection = db.GetCollection<Execution>();
            var previous = collection.Find(x => x.Instance.Tenant == execution.Instance.Tenant && x.Instance.Environment == execution.Instance.Environment);
            foreach (var p in previous)
            {
                if (p.State == Execution.ExecutionState.Added)
                {
                    p.State = Execution.ExecutionState.Declined;
                    collection.Update(p);
                }
            }

            collection.Insert(execution);

            using var session = queue.OpenSession();
            session.Enqueue(new ValueTuple<long>(execution.Id).Serialize());
            session.Flush();

            return Ok(execution.Id);
        }
    }
}