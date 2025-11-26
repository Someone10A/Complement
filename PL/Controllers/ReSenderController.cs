using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class ReSenderController : Controller
    {
        //ReSender
        [HttpGet]
        public IActionResult ReSender()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("ReSender");
        }
        [HttpPatch]
        public IActionResult Init57([FromBody]ML.ReSender.Ordenes orders) 
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            string response = BL.Resender.ReSender.Init57(orders.Dig);

            return Json(response);
        }
        [HttpPatch]
        public IActionResult Init13([FromBody] ML.ReSender.Ordenes orders) 
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            string response = BL.Resender.ReSender.Init13(orders.Dig);

            return Json(response);
        }
    }
}
