using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class OperatorController : Controller
    {
        string mode = "DEV";
        string cod_pto = "870";

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
        public IActionResult GetOrdersPerRoute([FromBody] ML.Operator.RouteHeader route)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.Operator.RouteDetail> routeDetailList = new List<ML.Operator.RouteDetail>();

            ML.Result result = BL.Operator.Operator.GetOrdersPerRoute(route, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            routeDetailList = (List<ML.Operator.RouteDetail>)result.Object;

            return View("Ruta", routeDetailList);
        }

        [HttpGet]
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
        public IActionResult AssignEvent([FromBody] ML.Operator.RouteHeader route,[FromBody] ML.Operator.RouteDetail routeDetail)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.Operator.Operator.AssignEvent(route, routeDetail,mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

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
