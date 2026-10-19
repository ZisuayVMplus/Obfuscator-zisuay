using geniussolution;
using geniussolution.Obfuscator;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace _77fuscatorWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ObfuscateController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ObfuscateController> _logger;
        private readonly RateLimiterService _rateLimiter;

        public ObfuscateController(IWebHostEnvironment env, ILogger<ObfuscateController> logger, RateLimiterService rateLimiter)
        {
            _env = env;
            _logger = logger;
            _rateLimiter = rateLimiter;
        }

        /// <summary>
        /// Obfuscates Luau/Lua source code.
        /// </summary>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Obfuscate([FromBody] ObfuscateRequest req)
        {
            // Rate limit by IP
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!_rateLimiter.Allow(ip))
                return StatusCode(429, new { error = "Rate limit exceeded. Please wait a moment." });

            if (string.IsNullOrWhiteSpace(req.Code))
                return BadRequest(new { error = "Code cannot be empty." });

            if (req.Code.Length > 500_000)
                return BadRequest(new { error = "Code too large (max 500,000 characters)." });

            // Use a unique temp directory per request to avoid collisions
            var jobId = Guid.NewGuid().ToString("N");
            var workDir = Path.Combine(Path.GetTempPath(), "77fuscator", jobId);
            Directory.CreateDirectory(workDir);

            var inputFile = Path.Combine(workDir, "input.lua");

            try
            {
                File.WriteAllText(inputFile, req.Code);

                var settings = new ObfuscationSettings
                {
                    EncryptStrings    = req.EncryptStrings ?? true,
                    ExtraCompression  = req.ExtraCompression ?? true,
                    EnhancedSecurity  = req.EnhancedSecurity ?? true,
                    DecryptTableLen   = req.DecryptTableLen ?? 500,
                    DynamicOpcodeStructure = req.DynamicOpcodeStructure ?? false,
                    Watermark         = req.Watermark ?? "77fuscator",
                };

                bool success = await Task.Run(() => _77F.Obfuscate(workDir, inputFile, settings, out var err));

                var outFile = Path.Combine(workDir, "out.lua");

                if (!success || !System.IO.File.Exists(outFile))
                {
                    _logger.LogWarning("Obfuscation failed for job {JobId}", jobId);
                    return StatusCode(500, new { error = "Obfuscation failed. Check that your code is valid Luau/Lua 5.1." });
                }

                var result = System.IO.File.ReadAllText(outFile);
                return Ok(new ObfuscateResponse { Output = result, JobId = jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during obfuscation job {JobId}", jobId);
                return StatusCode(500, new { error = "Internal server error." });
            }
            finally
            {
                // Clean up temp files
                try { Directory.Delete(workDir, recursive: true); } catch { }
            }
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    public class ObfuscateRequest
    {
        [Required]
        public string Code { get; set; } = "";

        // Optional settings (null = use defaults)
        public bool? EncryptStrings       { get; set; }
        public bool? ExtraCompression     { get; set; }
        public bool? EnhancedSecurity     { get; set; }
        public int?  DecryptTableLen      { get; set; }
        public bool? DynamicOpcodeStructure { get; set; }
        public string? Watermark          { get; set; }
    }

    public class ObfuscateResponse
    {
        public string Output { get; set; } = "";
        public string JobId  { get; set; } = "";
    }
}
