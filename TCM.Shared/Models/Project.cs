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

        public TestCaseParserType TestCaseParser { get; set; } = TestCaseParserType.None;
        public TestResultParserType TestResultParser { get; set; } = TestResultParserType.None;

        public List<Requirement> Requirements { get; set; } = new();
        public List<TestCase> TestCases { get; set; } = new();
        public List<TestRun> TestRuns { get; set; } = new();
    }
}