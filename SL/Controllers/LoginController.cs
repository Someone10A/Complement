using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;

namespace SL.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("api/Login/log")]
        [HttpGet]
        public IActionResult Login(ML.Login.Login login, string mode)
        {
            ML.Result result = BL.Login.Login.Log(login, mode);

            if(result.Correct)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result.Message);
            }
        }
    }
}
