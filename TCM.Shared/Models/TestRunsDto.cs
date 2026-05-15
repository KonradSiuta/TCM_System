using System.Collections.Generic;

namespace TCM.Shared.Models
{
    public class CreateTestRunDto
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string Environment { get; set; }
        public List<int> TestCaseIds { get; set; } = new();
    }
}