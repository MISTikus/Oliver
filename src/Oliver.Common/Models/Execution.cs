﻿namespace Oliver.Common.Models;

public record Execution
{
    public long Id { get; set; }
    public long TemplateId { get; set; }
    public long VariableSetId { get; set; }
    public ExecutionState State { get; set; }
    public byte RetryCount { get; set; }
    public Instance Instance { get; set; }
    public List<StepState> StepsStates { get; set; } = new List<StepState>();
    public Dictionary<string, string> VariableOverrides { get; set; } = new Dictionary<string, string>();

    public enum ExecutionState
    {
        Added = 0,
        Successed = 1,
        Failed = 2,
        Declined = 3,
        Retrying = 4
    }

    public record StepState
    {
        public int StepId { get; set; }
        public string StepName { get; set; }
        public string Executor { get; set; }
        public bool IsSuccess { get; set; }
        public List<string> Log { get; set; } = new List<string>();
    }

    public void Retrying()
    {
        RetryCount += 1;
        State = ExecutionState.Retrying;
    }

    public void Succeeded() => State = ExecutionState.Successed;
    public void Failed() => State = ExecutionState.Failed;
    public void Declined() => State = ExecutionState.Declined;
}
