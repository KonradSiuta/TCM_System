using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCM.Api.Data;
using System.Xml.Linq;
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
            if (dto.TestCaseIds == null || !dto.TestCaseIds.Any())
                return BadRequest("You must select at least one test case.");

            var testRun = new TestRun
            {
                ProjectId = dto.ProjectId,
                Name = dto.Name,
                Environment = dto.Environment,
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

        [HttpPatch("Execution/{executionId}/status")]
        public async Task<IActionResult> UpdateExecutionStatus(int executionId, [FromBody] ExecutionStatus status)
        {
            var execution = await _context.TestExecutions.FindAsync(executionId);
            if (execution == null) return NotFound();

            execution.Status = status;
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

            var project = await _context.Projects.FindAsync(testRun.ProjectId);
            if (project == null) return NotFound("Project not found.");

            string parserName = project.TestResultParser.ToString();

            if (project.TestResultParser == TestResultParserType.None)
                return BadRequest("This project has no Test Result Parser configured. Please set it to JUnitXml or EcuSimulatorXml.");

            if (!System.IO.File.Exists("report_parsers.json"))
                return BadRequest("report_parsers.json configuration file is missing.");

            var parsersJson = await System.IO.File.ReadAllTextAsync("report_parsers.json");
            var templates = System.Text.Json.JsonSerializer.Deserialize<List<ReportParserTemplate>>(parsersJson);
            var template = templates?.FirstOrDefault(t => t.Name == parserName);

            if (template == null) return BadRequest($"Report parser template '{parserName}' not found.");

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
    }
}