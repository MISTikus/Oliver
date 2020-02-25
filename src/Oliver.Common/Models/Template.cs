using System.Collections.Generic;

namespace Oliver.Common.Models
{
    public class Template
    {
        public List<Step> Steps { get; set; }

        public class Step
        {
            public int Id { get; set; }
            public StepType Type { get; set; }
            public string Command { get; set; }
        }

        public enum StepType
        {
            PShell,
            Docker,
            DockerCompose
        }
    }
}
