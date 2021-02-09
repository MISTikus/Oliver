using System.Collections.Generic;

namespace Oliver.Common.Models
{
    public class Template
    {
        public long Id { get; set; }
        public List<Step> Steps { get; set; } = new List<Step>();

        public class Step
        {
            public int Order { get; set; }
            public string Name { get; set; }
            public StepType Type { get; set; }
            public string Command { get; set; }
            public string WorkingFolder { get; set; } = "";
            public string FileName { get; set; }
        }

        public enum StepType : byte
        {
            Archive,
            PShell,
            CMD,
            Docker,
            DockerCompose
        }
    }
}
