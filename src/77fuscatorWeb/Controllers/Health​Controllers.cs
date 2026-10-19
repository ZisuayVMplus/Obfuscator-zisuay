using Microsoft.AspNetCore.Mvc;

namespace _77fuscatorWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok", version = "0.6.1" });
    }
}
