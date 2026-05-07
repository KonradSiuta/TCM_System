using System.Collections.Generic;

namespace TCM.Shared.Models
{
    public class Requirement
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }

        public string ExternalRequirementId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double Version { get; set; } = 1.0;
        public RequirementStatus Status { get; set; } = RequirementStatus.Created;

        public Project Project { get; set; }
        public List<RequirementCoverage> TestCaseCoverages { get; set; } = new();
    }
}