using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TCM.Api.Data;
using TCM.Api.Services.Parsers;
using TCM.Shared.Models;

namespace TCM.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TestCasesController : ControllerBase
    {
        private readonly TcmDbContext _context;

        public TestCasesController(TcmDbContext context)
        {
            _context = context;
        }

        [HttpGet("Project/{projectId}")]
        public async Task<ActionResult<IEnumerable<TestCase>>> GetProjectTestCases(int projectId)
        {
            var testCases = await _context.TestCases
                .Include(t => t.RequirementCoverages)
                    .ThenInclude(rc => rc.Requirement)
                .Where(t => t.ProjectId == projectId && t.IsActive)
                .ToListAsync();

            return Ok(testCases);
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateTestCase(int id)
        {
            var testCase = await _context.TestCases.FindAsync(id);
            if (testCase == null) return NotFound();

            testCase.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("Project/{projectId}/import-source")]
        public async Task<IActionResult> ImportFromSource(int projectId, IFormFile file)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound("Project not found.");

            string parserName = project.ParserName;

            var parsersJson = await System.IO.File.ReadAllTextAsync("parsers.json");
            var templates = System.Text.Json.JsonSerializer.Deserialize<List<ParserTemplate>>(parsersJson);
            var template = templates.FirstOrDefault(t => t.Name == parserName);

            if (template == null) return BadRequest($"Parser template '{parserName}' not found in parsers.json.");

            string fileContent;
            using (var reader = new StreamReader(file.OpenReadStream())) { fileContent = await reader.ReadToEndAsync(); }

            var parserEngine = new DynamicRegexParser(template);
            var parsedDtos = parserEngine.Parse(fileContent, projectId);

            int added = 0, updated = 0, linkedReqs = 0;

            var projectReqs = await _context.Requirements
                .Where(r => r.ProjectId == projectId)
                .ToListAsync();

            foreach (var dto in parsedDtos)
            {
                var existingTc = await _context.TestCases
                    .Include(t => t.Steps)
                    .FirstOrDefaultAsync(t => t.ExternalId == dto.TestCase.ExternalId && t.ProjectId == projectId);

                if (existingTc != null)
                {
                    _context.TestSteps.RemoveRange(existingTc.Steps);

                    existingTc.Title = dto.TestCase.Title;
                    existingTc.Description = dto.TestCase.Description;
                    existingTc.Version = dto.TestCase.Version;
                    existingTc.Type = dto.TestCase.Type;
                    existingTc.Source = dto.TestCase.Source;
                    existingTc.Steps = dto.TestCase.Steps;
                    existingTc.IsActive = true;

                    var existingCoverages = await _context.RequirementCoverages
                        .Where(rc => rc.TestCaseId == existingTc.Id)
                        .ToListAsync();
                    _context.RequirementCoverages.RemoveRange(existingCoverages);

                    foreach (var cr in dto.CoveredRequirements)
                    {
                        var req = projectReqs.FirstOrDefault(r => r.ExternalRequirementId == cr.ExternalId);
                        if (req != null)
                        {
                            _context.RequirementCoverages.Add(new RequirementCoverage
                            {
                                TestCaseId = existingTc.Id,
                                RequirementId = req.Id,
                                CoveredVersion = cr.Version
                            });
                            linkedReqs++;
                        }
                    }
                    updated++;
                }
                else
                {
                    _context.TestCases.Add(dto.TestCase);

                    foreach (var cr in dto.CoveredRequirements)
                    {
                        var req = projectReqs.FirstOrDefault(r => r.ExternalRequirementId == cr.ExternalId);
                        if (req != null)
                        {
                            _context.RequirementCoverages.Add(new RequirementCoverage
                            {
                                TestCase = dto.TestCase,
                                RequirementId = req.Id,
                                CoveredVersion = cr.Version
                            });
                            linkedReqs++;
                        }
                    }
                    added++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok($"Import finished!\n- Added newly: {added}\n- Updated: {updated}\n- Linked Requirements: {linkedReqs}");
        }
    }
}