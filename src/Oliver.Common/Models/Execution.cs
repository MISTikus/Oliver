using System.Collections.Generic;

namespace Oliver.Common.Models
{
    public class Execution
    {
        public long Id { get; set; }
        public long TemplateId { get; set; }
        public ExecutionState State { get; set; }
        public List<StepState> StepsStates { get; set; }
        public Instance Instance { get; set; }
        public Dictionary<string, string> Variables { get; set; }

        public enum ExecutionState
        {
            Added = 0,
            Successed = 1,
            Failed = 2,
            Declined = 3
        }

        public class StepState
        {
            public int StepId { get; set; }
            public string Executor { get; set; }
            public bool IsSuccessed { get; set; }
            public string[] Log { get; set; }
        }
    }
}
