using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TCM.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ParsersController : ControllerBase
    {
        private readonly string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "Parsers");

        public ParsersController()
        {
            Directory.CreateDirectory(Path.Combine(_basePath, "TestCases"));
            Directory.CreateDirectory(Path.Combine(_basePath, "TestResults"));
        }

        [HttpGet("{type}")]
        public IActionResult GetParsers(string type)
        {
            if (type != "TestCases" && type != "TestResults") return BadRequest("Invalid parser type.");

            var dir = Path.Combine(_basePath, type);
            var files = Directory.GetFiles(dir, "*.json").Select(Path.GetFileNameWithoutExtension).ToList();

            if (!files.Any())
            {
                files = type == "TestCases" ? new List<string> { "DoxygenStyle", "GTest" } : new List<string> { "JUnitXML", "NUnitXML" };
            }

            return Ok(files);
        }

        [HttpPost("{type}/upload")]
        public async Task<IActionResult> UploadParser(string type, IFormFile file)
        {
            if (type != "TestCases" && type != "TestResults") return BadRequest("Invalid parser type.");
            if (file == null || file.Length == 0) return BadRequest("File is empty.");
            if (Path.GetExtension(file.FileName).ToLower() != ".json") return BadRequest("Only JSON files are allowed.");

            var filePath = Path.Combine(_basePath, type, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(Path.GetFileNameWithoutExtension(file.FileName));
        }
    }
}