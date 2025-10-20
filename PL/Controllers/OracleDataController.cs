using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class OracleDataController : Controller
    {
        private readonly IConfiguration _config;

        public OracleDataController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult GetCredentials()
        {
            var username = _config["OracleData:Username"];
            var password = _config["OracleData:Password"];

            return Json(new { username, password });
        }


        [HttpGet]
        public IActionResult GetEndPointOLPN()
        {
            var endPoint = _config["OracleData:EndPointOLPN"];

            return Json(new { endPoint });
        }


    }
}
