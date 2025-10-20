using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using ML.Maintenance;

namespace PL.Controllers
{
    public class MaintenanceController : Controller
    {
        string mode = "DEV";
        string cod_pto = "870";

        [HttpGet]
        public IActionResult InfoByScn()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("Maintenance");
        }


        [HttpPost]
        public IActionResult GetScnInfo(string numScn)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            var result = BL.Maintenance.Maintenance.GetScnInfo(numScn, mode);

            ML.Maintenance.InfoByScn infoByScn = new ML.Maintenance.InfoByScn();
            if (!result.Correct)
            {
                return BadRequest($@"{result.Message}");
            }
            infoByScn = (ML.Maintenance.InfoByScn)result.Object;

            return View("Maintenance", infoByScn); 
        }

        [HttpPost]
        public IActionResult UpdateScnInfo([FromBody]ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.Maintenance.Maintenance.UpdateScnInfo(confirmedInfo, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
    }
}
