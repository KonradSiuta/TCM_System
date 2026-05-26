using System.Collections.Generic;

namespace TCM.Shared.Models
{
    public class CreateTestRunDto
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string Environment { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public List<int> TestCaseIds { get; set; } = new();
    }

    public class UpdateTestRunDto
    {
        public string Name { get; set; }
        public string Environment { get; set; }
        public DateTime? PlannedEndDate { get; set; }
    }

    public class UpdateExecutionDto
    {
        public ExecutionStatus Status { get; set; }
        public string Notes { get; set; }
        public string DefectUrl { get; set; }
    }
}