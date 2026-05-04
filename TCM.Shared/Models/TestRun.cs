namespace TCM.Shared.Models
{
    public class TestRun
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string Environment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Project Project { get; set; }
        public List<TestExecution> Executions { get; set; } = new();
    }
}