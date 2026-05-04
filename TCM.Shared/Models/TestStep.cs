namespace TCM.Shared.Models
{
    public class TestStep
    {
        public int Id { get; set; }
        public int TestCaseId { get; set; }
        public int StepNumber { get; set; }
        public string Action { get; set; }
        public string ExpectedResult { get; set; }

        public TestCase TestCase { get; set; }
    }
}