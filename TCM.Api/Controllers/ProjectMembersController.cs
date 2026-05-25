using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCM.Api.Data;
using TCM.Shared.Models;

namespace TCM.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectMembersController : ControllerBase
    {
        private readonly TcmDbContext _context;

        public ProjectMembersController(TcmDbContext context)
        {
            _context = context;
        }

        [HttpGet("Project/{projectId}")]
        public async Task<ActionResult<IEnumerable<ProjectMemberDto>>> GetProjectMembers(int projectId)
        {
            var members = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == projectId)
                .Select(pm => new ProjectMemberDto
                {
                    UserId = pm.UserId,
                    Username = pm.User.Username,
                    Email = pm.User.Email,
                    Role = pm.Role
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertProjectMember(UpsertMemberDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null) return NotFound($"User '{dto.Username}' does not exist in the system.");

            var existingMember = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == dto.ProjectId && pm.UserId == user.Id);

            if (existingMember != null)
            {
                existingMember.Role = dto.Role;
                await _context.SaveChangesAsync();
                return Ok($"User '{dto.Username}' role updated to {dto.Role}.");
            }
            else
            {
                var newMember = new ProjectMember
                {
                    ProjectId = dto.ProjectId,
                    UserId = user.Id,
                    Role = dto.Role
                };
                _context.ProjectMembers.Add(newMember);
                await _context.SaveChangesAsync();
                return Ok($"User '{dto.Username}' added to the project as {dto.Role}.");
            }
        }

        [HttpDelete("Project/{projectId}/User/{userId}")]
        public async Task<IActionResult> RemoveMember(int projectId, int userId)
        {
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

            if (member == null) return NotFound();

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}