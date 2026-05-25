using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
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

        [HttpPost("Project/{projectId}/import")]
        public async Task<IActionResult> ImportFromCsv(int projectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var callerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(callerIdStr, out int callerId)) return Unauthorized();

            var member = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == callerId);
            if (member == null || member.Role != ProjectRole.Administrator)
                return StatusCode(403, "Only Administrators can import requirements.");

            var requirementsToUpdate = new List<Requirement>();
            var requirementsToAdd = new List<Requirement>();

            int skippedCount = 0;
            var errorLog = new List<string>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                await reader.ReadLineAsync();

                int lineNumber = 1;
                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(';');
                    if (values.Length < 6)
                    {
                        errorLog.Add($"Line {lineNumber}: Not enough columns. Expected 6.");
                        continue;
                    }

                    int.TryParse(values[0].Trim(), out int reqId);
                    string externalId = values[1].Trim();

                    string title = values[2].Trim();
                    if (string.IsNullOrEmpty(title))
                    {
                        errorLog.Add($"Line {lineNumber}: Title cannot be empty.");
                        continue;
                    }

                    string description = values[3].Trim();

                    if (!double.TryParse(values[4].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedVersion))
                    {
                        errorLog.Add($"Line {lineNumber}: Version must be a valid number (e.g., 1.5).");
                        continue;
                    }

                    if (!Enum.TryParse<RequirementStatus>(values[5].Trim(), true, out var parsedStatus))
                    {
                        errorLog.Add($"Line {lineNumber}: Invalid Status.");
                        continue;
                    }

                    Requirement existingReq = null;

                    if (reqId > 0)
                    {
                        existingReq = await _context.Requirements
                            .FirstOrDefaultAsync(r => r.Id == reqId && r.ProjectId == projectId);
                    }

                    if (existingReq == null && !string.IsNullOrWhiteSpace(externalId))
                    {
                        existingReq = await _context.Requirements
                            .FirstOrDefaultAsync(r => r.ExternalRequirementId == externalId && r.ProjectId == projectId);
                    }

                    if (existingReq != null)
                    {
                        if (parsedVersion > existingReq.Version)
                        {
                            existingReq.ExternalRequirementId = externalId;
                            existingReq.Title = title;
                            existingReq.Description = description;
                            existingReq.Version = parsedVersion;
                            existingReq.Status = parsedStatus;

                            requirementsToUpdate.Add(existingReq);
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    else
                    {
                        requirementsToAdd.Add(new Requirement
                        {
                            ProjectId = projectId,
                            ExternalRequirementId = externalId,
                            Title = title,
                            Description = description,
                            Version = parsedVersion,
                            Status = parsedStatus
                        });
                    }
                }
            }

            if (requirementsToAdd.Any()) _context.Requirements.AddRange(requirementsToAdd);
            if (requirementsToUpdate.Any()) _context.Requirements.UpdateRange(requirementsToUpdate);

            await _context.SaveChangesAsync();

            string summary = $"Import finished!\n" +
                             $"- Added newly: {requirementsToAdd.Count}\n" +
                             $"- Updated: {requirementsToUpdate.Count}\n" +
                             $"- Skipped (older/same version): {skippedCount}";

            if (errorLog.Any())
            {
                summary += $"\n\nErrors encountered ({errorLog.Count}):\n" + string.Join("\n", errorLog.Take(5));
                if (errorLog.Count > 5) summary += "\n...and more.";
            }

            return Ok(summary);
        }
    }
}