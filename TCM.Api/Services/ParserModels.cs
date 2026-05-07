using System.Collections.Generic;
using TCM.Shared.Models;

namespace TCM.Api.Services.Parsers
{
    public class RequirementCoverageDto
    {
        public string ExternalId { get; set; }
        public double Version { get; set; }
    }

    public class TestCaseImportDto
    {
        public TestCase TestCase { get; set; }
        public List<RequirementCoverageDto> CoveredRequirements { get; set; } = new();
    }

    public class ParserTemplate
    {
        public string Name { get; set; }
        public string BlockRegex { get; set; }
        public string IdRegex { get; set; }
        public string TitleRegex { get; set; }
        public string DescRegex { get; set; }
        public string VersionRegex { get; set; }
        public string TypeRegex { get; set; }
        public string SourceRegex { get; set; }
        public string RequirementRegex { get; set; }
        public string StepRegex { get; set; }
    }
}