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
        public string PriorityRegex { get; set; }
    }

    public class ReportParserTemplate
    {
        public string Name { get; set; }
        public string TestCaseNode { get; set; }
        public string IdentityAttribute { get; set; }
        public string SecondaryIdentityAttribute { get; set; }
        public string FailureNode { get; set; }
        public string FailureMessageAttribute { get; set; }
        public string SkippedNode { get; set; }
    }
}