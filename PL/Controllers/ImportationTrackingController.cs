using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class ImportationTrackingController : Controller
    {
        string mode = "PRO";
        public IActionResult ImportationTracking()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetOrdersToPrint()
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            string cod_pto = HttpContext.Session.GetString("pto_alm");


            ML.Result result = BL.ImportationTracking.ImportationTracking.GetOrdersToPrint(mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message,
                data = result.Object
            });
        }

        [HttpGet]
        public IActionResult EvaluateOC(string noOrden)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            string cod_pto = HttpContext.Session.GetString("pto_alm");


            ML.Result result = BL.ImportationTracking.ImportationTracking.EvaluateOC(noOrden, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message,
                data = result.Object
            });
        }


        [HttpPost]
        public IActionResult GetCanguroInfo([FromBody] ML.ImportationTracking.OrdenCompra ordenCompra)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.ImportationTracking.ImportationTracking.Generate(ordenCompra, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult PrintOrder([FromBody] ML.ImportationTracking.PtrInfo ptrInfo)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }
            /*LOS UNICOS VALORES DE ptrInfo.Ptr son 
             ptrdort1
            ptr021e2
                limitar al usuario que solo elija una de esas 2 en un dropDownList*/

            ML.Result result = BL.ImportationTracking.ImportationTracking.PrintOrder(ptrInfo, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message
            });
        }

    }
}
