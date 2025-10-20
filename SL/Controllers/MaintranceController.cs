using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ML;

namespace SL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintranceController : ControllerBase
    {
        [HttpPost("get-rdd-info")]
        public IActionResult GetRddInfo()
        {
            return Ok();
        }

        [HttpPost("update-rdd")]
        public IActionResult UpdateRdd()
        {
            return Ok();
        }
    }
}
