using Microsoft.AspNetCore.Mvc;
using ML.Operator;
using Newtonsoft.Json;

namespace PL.Controllers
{
    public class OperatorController : Controller
    {
        string mode = "DEV";
        string cod_pto = "870";
        /*Asignado*/
        [HttpGet]
        public IActionResult GetAssignedRoute()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.Operator.Operator.GetAssignedRoute(usuId, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
                return View("Asignado", new List<ML.Operator.RouteHeader>());
            }

            return View("Asignado", result.Object);
        }
        [HttpPost]
        public IActionResult AcceptRoute([FromBody] ML.Operator.RouteHeader route)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.Operator.Operator.AcceptRoute(route, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        [HttpPost]
        public IActionResult FinishRoute([FromBody] ML.Operator.RouteHeader route)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.Operator.Operator.FinishRoute(route, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        /*Ruta*/
        [HttpPost]
        public IActionResult GetOrdersPerRoute(string routeJson)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            if (string.IsNullOrEmpty(routeJson))
            {
                return RedirectToAction("GetAssignedRoute");
            }

            var route = JsonConvert.DeserializeObject<ML.Operator.RouteHeader>(routeJson);
            //TempData["RouteHeader"] = JsonConvert.SerializeObject(route);
            HttpContext.Session.SetString("RouteHeader", routeJson);

            return RedirectToAction("RutaOperador");
        }

        [HttpGet]
        public IActionResult RutaOperador()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            var json = HttpContext.Session.GetString("RouteHeader");
            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("GetAssignedRoute");
            }

            List<ML.Operator.RouteDetail> routeDetailList = new List<ML.Operator.RouteDetail>();

            var route = JsonConvert.DeserializeObject<ML.Operator.RouteHeader>(json);

            ML.Result result = BL.Operator.Operator.GetOrdersPerRoute(route, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            routeDetailList = (List<ML.Operator.RouteDetail>)result.Object;

            ViewBag.RouteHeader = route;

            //string debugJson = JsonConvert.SerializeObject(routeDetailList, Formatting.Indented);

            return View(routeDetailList);
        }
        [HttpGet]
        public IActionResult GetReasons()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            var result = BL.Operator.Operator.GetReasons(mode);
            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }

            return Json(result.Object);
        }
        [HttpPost]
        public IActionResult AssignEvent([FromBody] ML.Operator.RouteRequest routeRequest)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.Operator.Operator.AssignEvent(routeRequest.Header, routeRequest.Detail, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        /*Historico*/
        [HttpGet]
        public IActionResult GetHistorical()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.Operator.Operator.GetHistorical(usuId, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
                return View("Historico", new List<ML.Operator.RouteHeader>());
            }

            return View("Historico", result.Object);
        }

    }
}
