using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using ML.ImportationTracking;

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

            ML.Result result = BL.ImportationTracking.ImportationTracking.PrintOrder(ptrInfo, false, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult RePrintOrder([FromBody] ML.ImportationTracking.PtrInfo ptrInfo)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.ImportationTracking.ImportationTracking.RePrintOrder(ptrInfo, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message
            });
        }


        [HttpGet]
        public IActionResult GetPrinters()
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }
            string codPto = HttpContext.Session.GetString("pto_alm");

            ML.Result result = BL.ImportationTracking.ImportationTracking.GetPrinters(codPto, mode);

            return Json(new
            {
                correct = result.Correct,
                data = result.Object
            });
        }
    }
}
