using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
        public List<RequirementCoverage> RequirementCoverages { get; set; } = new();
        public List<TestStep> Steps { get; set; } = new();
        public List<TestExecution> Executions { get; set; } = new();

        [NotMapped]
        public string CoveredRequirementsText => RequirementCoverages != null && RequirementCoverages.Any()
            ? string.Join(", ", RequirementCoverages.Select(rc => rc.Requirement?.ExternalRequirementId))
            : "None";
    }
}