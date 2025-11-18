using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ML.DeliveryPlanner;


namespace PL.Controllers
{
    public class DeliveryPlannerController : Controller
    {
        string mode = "DEV";

        [HttpGet]
        public IActionResult GetReadyOrdersPerDate()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("ReadyOrders");
        }

        [HttpPost]
        public IActionResult GetReadyOrdersPerDate([FromBody] ML.DeliveryPlanner.ReadyQuery readyQuery)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.DeliveryPlanner.ReadyInfo> readyInfoList = new List<ML.DeliveryPlanner.ReadyInfo>();

            ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.GetReadyOrdersPerDate(readyQuery, mode);
            if (!result.Correct)
            {
                return BadRequest($@"{result.Message}");
            }
            readyInfoList = (List<ML.DeliveryPlanner.ReadyInfo>)result.Object;

            return Json(readyInfoList);
        }

        [HttpPost]
        public IActionResult SendReadyOrders([FromBody] List<ML.DeliveryPlanner.ReadyInfo> readyInfoList)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.OrderFreeze(readyInfoList, mode);

            return Json(result);
        }

        [HttpGet]
        public IActionResult Planning()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("Planning");
        }


        [HttpPost]
        public IActionResult GetOrdersPerDate([FromBody] ML.DeliveryPlanner.PlanQuery planQuery)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.DeliveryPlanner.PlanInfo> planInfoList = new List<ML.DeliveryPlanner.PlanInfo>();

            ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.GetOrdersPerDate(planQuery, mode);
            if (!result.Correct)
            {
                return BadRequest($@"{result.Message}");
            }
            planInfoList = (List<ML.DeliveryPlanner.PlanInfo>)result.Object;

            return Json(planInfoList);
        }
    }
}
