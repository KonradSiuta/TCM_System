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

            int added = 0, updated = 0;

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
                    updated++;

                    // TODO: dodać aktualizację tabeli RequirementCoverage 
                }
                else
                {
                    _context.TestCases.Add(dto.TestCase);
                    added++;
                }
            }

            await _context.SaveChangesAsync();
            return Ok($"Imported: {added}, Updated: {updated}");
        }
    }
}