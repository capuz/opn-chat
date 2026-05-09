using Microsoft.AspNetCore.Mvc;

namespace opn_chat.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new 
            { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                architecture = "Clean Architecture - 4 layers"
            });
        }
    }
}
