using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCM.Api.Data;
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
        public async Task<ActionResult<TestRun>> CreateTestRun(TestRun testRun)
        {
            var activeTestCases = await _context.TestCases
                .Where(tc => tc.ProjectId == testRun.ProjectId && tc.IsActive)
                .ToListAsync();

            if (!activeTestCases.Any())
                return BadRequest("No active test cases found to create a run.");

            testRun.CreatedAt = DateTime.UtcNow;
            _context.TestRuns.Add(testRun);
            await _context.SaveChangesAsync();

            var executions = activeTestCases.Select(tc => new TestExecution
            {
                TestRunId = testRun.Id,
                TestCaseId = tc.Id,
                Status = ExecutionStatus.Unexecuted,
                Mode = tc.Type == TestType.Unit ? ExecutionMode.Automated : ExecutionMode.Manual
            }).ToList();

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
    }
}