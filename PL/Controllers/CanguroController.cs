using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace PL.Controllers
{
    public class CanguroController : Controller
    {
        string mode = "DEV";
        public IActionResult Canguro()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetWorkList()
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            string cod_pto = HttpContext.Session.GetString("pto_alm");


            ML.Result result = BL.Canguro.Canguro.GetWorkList(cod_pto, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message,
                data = result.Object
            });
        }

        [HttpPost]
        public IActionResult GetCanguroInfo([FromBody] ML.Canguro.CanguroInfo canguro)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.Canguro.Canguro.GetCanguroInfo(canguro, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message,
                data = result.Object
            });
        }

        [HttpPost]
        public IActionResult AcceptCanguro([FromBody] ML.Canguro.CanguroInfo canguro)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }


            ML.Result result = BL.Canguro.Canguro.AcceptCanguro(canguro, usuId, mode);

            //Logica

            return Json(new
            {
                correct = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult Maintenance([FromBody] ML.Canguro.Maintenance maintenance)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            // Validar fecha de entrega
            if (!string.IsNullOrEmpty(maintenance.Header?.FechaEntrega))
            {
                DateTime fechaEntrega;
                bool formatoValido = DateTime.TryParseExact(
                    maintenance.Header.FechaEntrega,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out fechaEntrega
                );

                if (!formatoValido)
                {
                    return Json(new
                    {
                        correct = false,
                        message = "Formato de fecha inválido. Use yyyy-MM-dd."
                    });
                }

                if (fechaEntrega.Date < DateTime.Now.Date)
                {
                    return Json(new
                    {
                        correct = false,
                        message = "La fecha de entrega no puede ser anterior al día actual."
                    });
                }
            }
            else
            {
                return Json(new
                {
                    correct = false,
                    message = "La fecha de entrega es requerida."
                });
            }


            ML.Result result = BL.Canguro.Canguro.Maintenance(maintenance, usuId, mode);

            return Json(new
            {
                correct = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult Delivered([FromBody] ML.Canguro.Delivered Delivered)
        {
            string usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }


            ML.Result result = BL.Canguro.Canguro.Delivered(Delivered, usuId, mode);

            
            return Json(new
            {
                correct = result.Correct,
                message = result.Message
            });
        }

    }
}
