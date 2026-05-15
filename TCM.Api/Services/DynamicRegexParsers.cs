using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TCM.Shared.Models;

namespace TCM.Api.Services.Parsers
{
    public class DynamicRegexParser
    {
        private readonly ParserTemplate _template;

        public DynamicRegexParser(ParserTemplate template)
        {
            _template = template;
        }

        public List<TestCaseImportDto> Parse(string content, int projectId)
        {
            var results = new List<TestCaseImportDto>();
            var blockRegex = new Regex(_template.BlockRegex, RegexOptions.Singleline);
            var blocks = blockRegex.Matches(content);

            foreach (Match block in blocks)
            {
                string comment = block.Groups[1].Value;
                string extId = GetTagValue(comment, _template.IdRegex);

                if (!string.IsNullOrEmpty(extId))
                {
                    var dto = new TestCaseImportDto
                    {
                        TestCase = new TestCase
                        {
                            ProjectId = projectId,
                            ExternalId = extId,
                            Title = GetTagValue(comment, _template.TitleRegex) ?? "Untitled",
                            Description = GetTagValue(comment, _template.DescRegex) ?? "",
                            Version = GetTagValue(comment, _template.VersionRegex) ?? "1.0",
                            IsReadOnly = true
                        }
                    };

                    Enum.TryParse<TestType>(GetTagValue(comment, _template.TypeRegex), true, out var tType);
                    dto.TestCase.Type = tType;

                    Enum.TryParse<TestPriority>(GetTagValue(comment, _template.PriorityRegex), true, out var tPriority);
                    dto.TestCase.Priority = tPriority;

                    Enum.TryParse<TestCaseSource>(GetTagValue(comment, _template.SourceRegex), true, out var tSource);
                    dto.TestCase.Source = tSource;

                    string reqLine = GetTagValue(comment, _template.RequirementRegex);
                    if (!string.IsNullOrWhiteSpace(reqLine))
                    {
                        var reqEntries = reqLine.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var entry in reqEntries)
                        {
                            var parts = entry.Split(':', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2 && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double reqVer))
                            {
                                dto.CoveredRequirements.Add(new RequirementCoverageDto
                                {
                                    ExternalId = parts[0].Trim(),
                                    Version = reqVer
                                });
                            }
                        }
                    }

                    var stepMatches = Regex.Matches(comment, _template.StepRegex);
                    int stepNum = 1;
                    foreach (Match step in stepMatches)
                    {
                        dto.TestCase.Steps.Add(new TestStep
                        {
                            StepNumber = stepNum++,
                            Action = step.Groups[1].Value.Trim(),
                            ExpectedResult = step.Groups[2].Value.Trim()
                        });
                    }

                    results.Add(dto);
                }
            }
            return results;
        }

        private string GetTagValue(string text, string regexPattern)
        {
            if (string.IsNullOrEmpty(regexPattern)) return null;
            var match = Regex.Match(text, regexPattern);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}