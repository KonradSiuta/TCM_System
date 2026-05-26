using System.Collections.Generic;

namespace TCM.Shared.Models
{
    public class ProjectSummaryDto
    {
        public int TotalRequirements { get; set; }
        public int CoveredRequirements { get; set; }
        public double CoveragePercentage => TotalRequirements == 0 ? 0 : ((double)CoveredRequirements / TotalRequirements) * 100;

        public int TotalTestCases { get; set; }
        public int ActiveTestCases { get; set; }

        public int TotalTestRuns { get; set; }

        public List<OutdatedCoverageDto> OutdatedTestCases { get; set; } = new();

        public Dictionary<string, int> RequirementsByStatus { get; set; } = new();
        public Dictionary<string, int> TestCasesByPriority { get; set; } = new();
    }

    public class OutdatedCoverageDto
    {
        public string TestCaseExternalId { get; set; }
        public string TestCaseTitle { get; set; }
        public string RequirementExternalId { get; set; }
        public double CoveredVersion { get; set; }
        public double CurrentRequirementVersion { get; set; }
        public bool IsRequirementCanceled { get; set; }
    }
}