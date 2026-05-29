using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class ReshipmentController : Controller
    {
        string mode = "DEV";

        [HttpGet]
        public IActionResult Reshipment()
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View();
        }

        [HttpGet]
        public IActionResult GetReshipments()
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            string ptoAlm = HttpContext.Session.GetString("pto_alm");

            ML.Result result = BL.Reshipment.Reshipment.GetReshipments(ptoAlm, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message,
                data = result.Object //List<ML.Reshipment.Reshipment>
            });
        }

        [HttpGet]
        public IActionResult GetFacilities(string facility) //facility = campo facility del registro
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.Reshipment.Reshipment.GetFacilities(facility, mode);

                return Json(new
            {
                correct = result.Correct,
                message = result.Message,
                data = result.Object //List<(string facility, string desc)> facilityList
            });
        }


        [HttpPatch]
        public async Task<IActionResult> PatchLoadById([FromBody] ML.Reshipment.Reshipment reshipment)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }


            ML.Result result = await BL.Reshipment.Reshipment.PatchLoadById(reshipment, mode);

            return Json(new
            {
                correct = result.Correct, //bool
                message = result.Message //string
            });
        }


        [HttpPost]
        public async Task<IActionResult> ShipReshipment([FromBody] ML.Reshipment.Reshipment reshipment)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }


            ML.Result result = await BL.Reshipment.Reshipment.ShipReshipment(reshipment, usuId, mode);


            return Json(new
            {
                correct = result.Correct, //bool
                message = result.Message //string
            });
        }
    }
}
