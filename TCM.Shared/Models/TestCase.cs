using System.Collections.Generic;

namespace TCM.Shared.Models
{
    public class TestCase
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }

        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }

        public TestType Type { get; set; }
        public TestCaseSource Source { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsActive { get; set; } = true;

        public Project Project { get; set; }
        public List<Requirement> Requirements { get; set; } = new();
        public List<TestStep> Steps { get; set; } = new();
        public List<TestExecution> Executions { get; set; } = new();
    }
}