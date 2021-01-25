﻿using DiskQueue;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        public Task<ActionResult> GetExecution([FromRoute] long id)
        {
            using var db = this.databaseFactory();
            var collection = db.GetCollection<Execution>();
            var execution = collection.FindById(id);
            return execution is null
                ? Task.FromResult<ActionResult>(NotFound())
                : Task.FromResult<ActionResult>(Ok(execution));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AddExecutionStepLog([FromRoute] long id, [FromBody] Execution.StepState stepState,
            [FromQuery] Execution.ExecutionState? result)
        {
            this.logger.LogInformation("Received execution step log.");
            this.logger.LogInformation($"ExecutionId: '{id}'. Result: {result}.");
            this.logger.LogDebug($"Logs:");
            this.logger.LogDebug(string.Join('\n', stepState.Log));

            using var db = this.databaseFactory();
            var collection = db.GetCollection<Execution>();
            var execution = collection.FindById(id);
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
                                queues.Add(execution.Instance, this.queueFactory(execution.Instance));
                        }
                        var queue = queues[execution.Instance];
                        using var session = queue.OpenSession();
                        session.Enqueue(new ValueTuple<long>(execution.Id).Serialize());
                        session.Flush();
                        execution.RetryCount += 1;
                        execution.State = Execution.ExecutionState.Retrying;
                    }
                    else
                        execution.State = result.Value;
                }
                else
                    execution.State = result.Value;
            }

            collection.Update(execution);

            return Ok();
        }

        [HttpPost]
        public Task<ActionResult<long>> AddExecution([FromBody] Execution execution)
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
                if (p.State == Execution.ExecutionState.Added || p.State == Execution.ExecutionState.Retrying)
                {
                    p.State = Execution.ExecutionState.Declined;
                    collection.Update(p);
                }
            }

            collection.Insert(execution);

            using var session = queue.OpenSession();
            session.Enqueue(new ValueTuple<long>(execution.Id).Serialize());
            session.Flush();

            return Task.FromResult<ActionResult<long>>(Ok(execution.Id));
        }
    }
}