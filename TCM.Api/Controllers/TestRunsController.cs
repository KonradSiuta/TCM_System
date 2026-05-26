using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;
using TCM.Api.Data;
using TCM.Api.Services.Parsers;
using TCM.Shared.Models;

namespace TCM.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TestRunsController : ControllerBase
    {
        private readonly TcmDbContext _context;

        public TestRunsController(TcmDbContext context)
        {
            _context = context;
        }

        [HttpGet("Project/{projectId}")]
        public async Task<ActionResult<IEnumerable<TestRun>>> GetProjectTestRuns(int projectId)
        {
            return await _context.TestRuns
                .Where(tr => tr.ProjectId == projectId)
                .OrderByDescending(tr => tr.CreatedAt)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TestRun>> GetTestRun(int id)
        {
            var testRun = await _context.TestRuns
                .Include(tr => tr.Executions)
                    .ThenInclude(e => e.TestCase)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (testRun == null) return NotFound();
            return testRun;
        }

        [HttpPost]
        public async Task<ActionResult<TestRun>> CreateTestRun(CreateTestRunDto dto)
        {
            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(callerIdStr, out int callerId)) return Unauthorized();

            var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == dto.ProjectId && pm.UserId == callerId);
            if (member == null || member.Role != ProjectRole.Administrator)
                return StatusCode(403, "Only Administrators can create Test Runs.");

            if (dto.TestCaseIds == null || !dto.TestCaseIds.Any())
                return BadRequest("You must select at least one test case.");

            if (dto.TestCaseIds == null || !dto.TestCaseIds.Any())
                return BadRequest("You must select at least one test case.");

            var testRun = new TestRun
            {
                ProjectId = dto.ProjectId,
                Name = dto.Name,
                Environment = dto.Environment,
                PlannedEndDate = dto.PlannedEndDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.TestRuns.Add(testRun);
            await _context.SaveChangesAsync();

            var executions = new List<TestExecution>();
            foreach (var tcId in dto.TestCaseIds)
            {
                executions.Add(new TestExecution
                {
                    TestRunId = testRun.Id,
                    TestCaseId = tcId,
                    Status = ExecutionStatus.Unexecuted,
                    Mode = ExecutionMode.Manual,
                    DefectUrl = "",
                    Notes = ""
                });
            }

            _context.TestExecutions.AddRange(executions);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTestRun), new { id = testRun.Id }, testRun);
        }

        [HttpPut("Execution/{executionId}")]
        public async Task<IActionResult> UpdateExecution(int executionId, [FromBody] UpdateExecutionDto dto)
        {
            var execution = await _context.TestExecutions
                .Include(e => e.TestRun)
                .FirstOrDefaultAsync(e => e.Id == executionId);
            if (execution == null) return NotFound();

            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(callerIdStr, out int callerId)) return Unauthorized();

            var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == execution.TestRun.ProjectId && pm.UserId == callerId);
            if (member == null || member.Role == ProjectRole.Viewer)
                return StatusCode(403, "Viewers cannot modify execution status.");

            execution.Status = dto.Status;
            execution.Notes = dto.Notes ?? "";
            execution.DefectUrl = dto.DefectUrl ?? "";
            execution.ExecutedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{runId}/import-results")]
        public async Task<IActionResult> ImportTestResults(int runId, IFormFile file)
        {
            var testRun = await _context.TestRuns
                .Include(tr => tr.Executions)
                    .ThenInclude(e => e.TestCase)
                .FirstOrDefaultAsync(tr => tr.Id == runId);

            if (testRun == null) return NotFound("Test Run not found.");
            if (file == null || file.Length == 0) return BadRequest("File is empty.");

            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(callerIdStr, out int callerId))
            {
                var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == testRun.ProjectId && pm.UserId == callerId);
                if (member == null || member.Role == ProjectRole.Viewer)
                    return StatusCode(403, "Viewers cannot import test results.");
            }
            else return Unauthorized();

            var project = await _context.Projects.FindAsync(testRun.ProjectId);
            if (project == null) return NotFound("Project not found.");

            string parserName = project.TestResultParser;

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Parsers", "TestResults", $"{parserName}.json");

            if (!System.IO.File.Exists(filePath))
                return BadRequest($"Report parser configuration file '{parserName}.json' not found on the server.");

            var parsersJson = await System.IO.File.ReadAllTextAsync(filePath);
            var template = System.Text.Json.JsonSerializer.Deserialize<ReportParserTemplate>(parsersJson);

            if (template == null) return BadRequest($"Failed to load report parser template '{parserName}'.");

            try
            {
                using var stream = file.OpenReadStream();
                var doc = XDocument.Load(stream);

                var xmlTestCases = doc.Descendants(template.TestCaseNode).ToList();
                int passed = 0, failed = 0, skipped = 0;

                foreach (var execution in testRun.Executions)
                {
                    var externalId = execution.TestCase.ExternalId;

                    var xmlTest = xmlTestCases.FirstOrDefault(x =>
                        x.Attribute(template.IdentityAttribute)?.Value.Contains(externalId) == true ||
                        (!string.IsNullOrEmpty(template.SecondaryIdentityAttribute) && x.Attribute(template.SecondaryIdentityAttribute)?.Value.Contains(externalId) == true));

                    if (xmlTest != null)
                    {
                        var failureNode = xmlTest.Element(template.FailureNode);
                        var skippedNode = xmlTest.Element(template.SkippedNode);

                        if (failureNode != null)
                        {
                            execution.Status = ExecutionStatus.Failed;
                            execution.Notes = failureNode.Attribute(template.FailureMessageAttribute)?.Value ?? failureNode.Value;
                            failed++;
                        }
                        else if (skippedNode != null)
                        {
                            execution.Status = ExecutionStatus.Unexecuted;
                            execution.Notes = "Test skipped in automated run.";
                            skipped++;
                        }
                        else
                        {
                            execution.Status = ExecutionStatus.Passed;
                            execution.Notes = "Passed automatically.";
                            passed++;
                        }

                        execution.Mode = ExecutionMode.Automated;
                        execution.ExecutedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok($"Results imported successfully using '{parserName}'!\nPassed: {passed}, Failed: {failed}, Skipped: {skipped}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to parse the XML report: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTestRun(int id, [FromBody] UpdateTestRunDto dto)
        {
            var testRun = await _context.TestRuns.FindAsync(id);
            if (testRun == null) return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == testRun.ProjectId && pm.UserId == userId);

            if (member == null || member.Role != ProjectRole.Administrator)
                return StatusCode(403, "Only Administrators can modify Test Runs.");

            testRun.Name = dto.Name;
            testRun.Environment = dto.Environment;
            testRun.PlannedEndDate = dto.PlannedEndDate;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestRun(int id)
        {
            var testRun = await _context.TestRuns.FindAsync(id);
            if (testRun == null) return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == testRun.ProjectId && pm.UserId == userId);

            if (member == null || member.Role != ProjectRole.Administrator)
                return StatusCode(403, "Only Administrators can delete Test Runs.");

            _context.TestRuns.Remove(testRun);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{id}/pdf-report")]
        public async Task<IActionResult> GetTestRunPdfReport(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var testRun = await _context.TestRuns
                .Include(tr => tr.Project)
                .Include(tr => tr.Executions)
                    .ThenInclude(e => e.TestCase)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (testRun == null) return NotFound("Test Run not found.");

            int totalTests = testRun.Executions.Count;
            int passedTests = testRun.Executions.Count(e => e.Status == ExecutionStatus.Passed);
            int failedTests = testRun.Executions.Count(e => e.Status == ExecutionStatus.Failed);
            int blockedTests = testRun.Executions.Count(e => e.Status == ExecutionStatus.Blocked);
            int unexecutedTests = testRun.Executions.Count(e => e.Status == ExecutionStatus.Unexecuted);

            double successRate = totalTests == 0 ? 0 : ((double)passedTests / totalTests) * 100;

            var pdfData = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"TEST RUN REPORT").FontSize(22).Bold().FontColor("#007ACC");

                            row.ConstantItem(100).AlignRight().Text($"TCM SYSTEM").FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                        });

                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor("#007ACC");
                    });

                    page.Content().PaddingTop(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Grid(grid =>
                        {
                            grid.Columns(2);
                            grid.Item().Text(t => { t.Span("Project: ").Bold(); t.Span(testRun.Project?.Name ?? "N/A"); });
                            grid.Item().Text(t => { t.Span("Created At: ").Bold(); t.Span(testRun.CreatedAt.ToString("g")); });
                            grid.Item().Text(t => { t.Span("Test Cycle: ").Bold(); t.Span(testRun.Name); });
                            grid.Item().Text(t => { t.Span("Environment: ").Bold(); t.Span(testRun.Environment); });
                        });

                        col.Item().PaddingTop(20);

                        col.Item().Background(Colors.Grey.Lighten4).Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2).Grid(grid =>
                        {
                            grid.Columns(5);

                            grid.Item().Column(c => { c.Item().AlignCenter().Text("Total").Bold(); c.Item().AlignCenter().Text(totalTests.ToString()).FontSize(16).Bold(); });
                            grid.Item().Column(c => { c.Item().AlignCenter().Text("Passed").Bold(); c.Item().AlignCenter().Text(passedTests.ToString()).FontSize(16).Bold().FontColor(Colors.Green.Medium); });
                            grid.Item().Column(c => { c.Item().AlignCenter().Text("Failed").Bold(); c.Item().AlignCenter().Text(failedTests.ToString()).FontSize(16).Bold().FontColor(Colors.Red.Medium); });
                            grid.Item().Column(c => { c.Item().AlignCenter().Text("Blocked").Bold(); c.Item().AlignCenter().Text(blockedTests.ToString()).FontSize(16).Bold().FontColor(Colors.Amber.Medium); });
                            grid.Item().Column(c => { c.Item().AlignCenter().Text("Success Rate").Bold(); c.Item().AlignCenter().Text($"{successRate:F1}%").FontSize(16).Bold().FontColor("#007ACC"); });
                        });

                        col.Item().PaddingTop(25);
                        col.Item().Text("Detailed Execution Results").FontSize(14).Bold().FontColor("#333333");
                        col.Item().PaddingTop(5);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(60);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#007ACC").Padding(5).Text("Test ID").Bold().FontColor(Colors.White);
                                header.Cell().Background("#007ACC").Padding(5).Text("Title").Bold().FontColor(Colors.White);
                                header.Cell().Background("#007ACC").Padding(5).Text("Mode").Bold().FontColor(Colors.White);
                                header.Cell().Background("#007ACC").Padding(5).Text("Status").Bold().FontColor(Colors.White);
                                header.Cell().Background("#007ACC").Padding(5).Text("Notes").Bold().FontColor(Colors.White);
                                header.Cell().Background("#007ACC").Padding(5).Text("Defect URL").Bold().FontColor(Colors.White);
                            });

                            foreach (var exec in testRun.Executions)
                            {
                                string statusText = exec.Status.ToString();
                                string statusColor = exec.Status == ExecutionStatus.Passed ? "#28A745" :
                                                     exec.Status == ExecutionStatus.Failed ? "#DC3545" :
                                                     exec.Status == ExecutionStatus.Blocked ? "#FFC107" : "#6C757D";

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(exec.TestCase?.ExternalId ?? "N/A");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(exec.TestCase?.Title ?? "N/A");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(exec.Mode.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(statusText).Bold().FontColor(statusColor);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(string.IsNullOrWhiteSpace(exec.Notes) ? "-" : exec.Notes).FontSize(9);

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(string.IsNullOrWhiteSpace(exec.DefectUrl) ? "-" : exec.DefectUrl).FontSize(9).FontColor(Colors.Blue.Medium);
                            }
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();

            string safeFileName = $"Report_{testRun.Name.Replace(" ", "_")}.pdf";
            return File(pdfData, "application/pdf", safeFileName);
        }
    }
}