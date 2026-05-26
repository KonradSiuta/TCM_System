using System;
using System.Collections.Generic;

namespace TCM.Shared.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string ParserName { get; set; } = "DoxygenStyle";
        public string TestResultParser { get; set; } = "JUnitXML";

        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        public List<Requirement> Requirements { get; set; } = new();
        public List<TestCase> TestCases { get; set; } = new();
        public List<TestRun> TestRuns { get; set; } = new();
        public List<ProjectMember> Members { get; set; } = new();
    }
}