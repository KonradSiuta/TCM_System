using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCM.Api.Data;
using TCM.Shared.Models;
using Microsoft.AspNetCore.Authorization;

namespace TCM.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly TcmDbContext _context;

        public ProjectsController(TcmDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _context.Projects.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject(Project project)
        {
            project.CreatedAt = DateTime.UtcNow;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }

        [HttpGet("{id}/summary")]
        public async Task<ActionResult<ProjectSummaryDto>> GetProjectSummary(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            var summary = new ProjectSummaryDto();

            var requirements = await _context.Requirements
                .Include(r => r.TestCaseCoverages)
                .Where(r => r.ProjectId == id)
                .ToListAsync();

            summary.TotalRequirements = requirements.Count;
            summary.CoveredRequirements = requirements.Count(r => r.TestCaseCoverages.Any());

            var testCases = await _context.TestCases
                .Include(tc => tc.RequirementCoverages)
                    .ThenInclude(rc => rc.Requirement)
                .Where(tc => tc.ProjectId == id)
                .ToListAsync();

            summary.TotalTestCases = testCases.Count;
            summary.ActiveTestCases = testCases.Count(tc => tc.IsActive);

            summary.TotalTestRuns = await _context.TestRuns.CountAsync(tr => tr.ProjectId == id);

            foreach (var tc in testCases.Where(t => t.IsActive))
            {
                foreach (var cov in tc.RequirementCoverages)
                {
                    if (cov.CoveredVersion < cov.Requirement.Version)
                    {
                        summary.OutdatedTestCases.Add(new OutdatedCoverageDto
                        {
                            TestCaseExternalId = tc.ExternalId,
                            TestCaseTitle = tc.Title,
                            RequirementExternalId = cov.Requirement.ExternalRequirementId,
                            CoveredVersion = cov.CoveredVersion,
                            CurrentRequirementVersion = cov.Requirement.Version
                        });
                    }
                }
            }

            return Ok(summary);
        }
    }
}