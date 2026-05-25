using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCM.Api.Data;
using TCM.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(callerIdStr, out int callerId)) return Unauthorized();

            var projects = await _context.Projects
                .Where(p => p.Members.Any(pm => pm.UserId == callerId))
                .ToListAsync();

            return Ok(projects);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(callerIdStr, out int callerId)) return Unauthorized();

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            if (!project.Members.Any(pm => pm.UserId == callerId))
                return StatusCode(403, "You do not have access to this project.");

            return Ok(project);
        }

        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject(Project project)
        {
            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(callerIdStr, out int callerId)) return Unauthorized();

            project.CreatedAt = DateTime.UtcNow;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var member = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = callerId,
                Role = ProjectRole.Administrator
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }

        [HttpGet("{id}/summary")]
        public async Task<ActionResult<ProjectSummaryDto>> GetProjectSummary(int id)
        {
            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(callerIdStr, out int callerId)) return Unauthorized();

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            if (!project.Members.Any(pm => pm.UserId == callerId))
                return StatusCode(403, "You do not have access to this project's dashboard.");

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