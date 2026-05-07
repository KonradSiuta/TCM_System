namespace TCM.Shared.Models
{
    public class RequirementCoverage
    {
        public int RequirementId { get; set; }
        public Requirement Requirement { get; set; }

        public int TestCaseId { get; set; }
        public TestCase TestCase { get; set; }

        public double CoveredVersion { get; set; }
    }
}