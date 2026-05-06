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
    public class RequirementsController : ControllerBase
    {
        private readonly TcmDbContext _context;

        public RequirementsController(TcmDbContext context)
        {
            _context = context;
        }

        [HttpGet("Project/{projectId}")]
        public async Task<ActionResult<IEnumerable<Requirement>>> GetProjectRequirements(int projectId)
        {
            var requirements = await _context.Requirements
                .Where(r => r.ProjectId == projectId)
                .ToListAsync();

            return Ok(requirements);
        }

        [HttpPost]
        public async Task<ActionResult<Requirement>> CreateRequirement(Requirement requirement)
        {
            _context.Requirements.Add(requirement);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProjectRequirements), new { projectId = requirement.ProjectId }, requirement);
        }
    }
}