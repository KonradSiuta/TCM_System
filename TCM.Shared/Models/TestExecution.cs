namespace TCM.Shared.Models
{
    public class TestExecution
    {
        public int Id { get; set; }
        public int TestRunId { get; set; }
        public int TestCaseId { get; set; }

        public ExecutionStatus Status { get; set; }
        public ExecutionMode Mode { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public string DefectUrl { get; set; }
        public string Notes { get; set; }

        public TestRun TestRun { get; set; }
        public TestCase TestCase { get; set; }
    }
}