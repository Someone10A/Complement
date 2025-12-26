using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static NuGet.Packaging.PackagingConstants;

namespace PL.Controllers
{
    public class BaseControlController : Controller
    {
        string mode = "PRO";
        string cod_pto = "870";

        [HttpGet]
        public IActionResult BaseControl()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            var result = BL.BaseControl.BaseControl.GetOpenRoutes(cod_pto, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
                return View("BaseControl", new List<ML.BaseControl.OutboundShipment>());
            }

            return View("BaseControl", result.Object);
        }

        [HttpGet]
        public IActionResult BaseControlPast()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            var result = BL.BaseControl.BaseControl.GetOpenRoutesPast(cod_pto, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
                return View("BaseControlPast", new List<ML.BaseControl.OutboundShipment>());
            }

            return View("BaseControlPast", result.Object);
        }

        [HttpPost]
        public IActionResult GetOrdersPerRoute([FromBody] ML.BaseControl.OutboundShipment outboundShipment)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            var result = BL.BaseControl.BaseControl.GetOrdersPerRoute(outboundShipment, mode);
            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }
            //Regresa List<ML.BaseControl.Order> orderList 
            return Json(result.Object);
        }

        [HttpGet]
        public IActionResult GetOrdersPerData()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("GetOrdersPerData");
        }
        [HttpPost]
        public IActionResult GetOrdersPerData([FromBody] ML.BaseControl.QueryInfo queryInfo)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            queryInfo.pto_alm = cod_pto;
            var result = BL.BaseControl.BaseControl.GetOrdersPerData(queryInfo, mode);
            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }

            return Json(result.Object);
        }

        public IActionResult GetReasons()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            var result = BL.BaseControl.BaseControl.GetReasons(mode);
            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }

            return Json(result.Object);
        }
        
        [HttpPost]
        public async Task<IActionResult> Confirmation([FromBody] ML.BaseControl.Confirmation confirmation)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            confirmation.User = usuId;

            var result = await BL.BaseControl.BaseControl.Confirmation(confirmation, mode);

            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Message);
        }

        //MultiConfirmationsOk
        [HttpPost]
        public async Task<IActionResult> xMultiConfirmation(string shipment, string mode)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            var result = await BL.BaseControl.BaseControl.MultiConfirmationsOk(usuId, shipment, cod_pto, mode);

            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Message);
        }
        //-------------------------------------------------------------------
        //-------------------BASE OPERATOR-----------------------------------
        //-------------------------------------------------------------------
        [HttpGet]
        public IActionResult BaseOperatorGetOpenRoutes()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            var result = BL.BaseControl.BaseOperator.GetOpenRoutes("DEV"); 
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
                return View("BaseOperator", new List<ML.Operator.RouteHeader>());
            }

            return View("BaseOperator", result.Object);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult BaseOperatorGetOrdersPerRoute([FromBody] ML.Operator.RouteHeader route)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            var result = BL.BaseControl.BaseOperator.GetOrdersPerRoute(route, "DEV");
            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }

            string debugJson = JsonConvert.SerializeObject(result.Object, Formatting.Indented);
            return Json(result.Object);
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> MultiConfirmation([FromBody] ML.Operator.RouteHeader route)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = await BL.BaseControl.BaseOperator.MultiConfirmation(route, usuId, "DEV");

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult RejectRoute([FromBody] ML.Operator.RouteHeader route)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.BaseControl.BaseOperator.RejectRoute(route, "DEV");

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
    }
}
