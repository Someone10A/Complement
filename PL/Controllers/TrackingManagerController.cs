using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static NuGet.Packaging.PackagingConstants;

namespace PL.Controllers
{
    public class TrackingManagerController : Controller
    {
        string mode = "PRO";
        string cod_pto = "870";

        [HttpGet]
        public IActionResult GetTrackingPerDay()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            DateTime today = DateTime.Today;

            List<ML.TrackingManager.OutboundShipment> routeList = new List<ML.TrackingManager.OutboundShipment>();
            ML.Result result = BL.TrackingManager.TrackingManager.GetTrackingPerDay(today.ToString("ddMMyyyy"), cod_pto, mode);

            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
                return View("Rutas", new List<ML.TrackingManager.OutboundShipment>());
            }

            routeList = (List<ML.TrackingManager.OutboundShipment>)result.Object;
            return View("Rutas", routeList);
        }

        [HttpPost]
        public IActionResult GetTrackingPerDay([FromBody] ML.TrackingManager.DateRequest request)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }
            List<ML.TrackingManager.OutboundShipment> routeList = new List<ML.TrackingManager.OutboundShipment>();

            ML.Result result = BL.TrackingManager.TrackingManager.GetTrackingPerDay(request.date, cod_pto, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            routeList = (List<ML.TrackingManager.OutboundShipment>)result.Object;

            //return View("Rutas", routeList);
            return Json(routeList);
        }

        [HttpPost]
        public IActionResult GetOrdersPerOutboundShipment(ML.TrackingManager.OutboundShipment outboundShipment)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }
            List<ML.TrackingManager.TrackingManager> trackingManagertList = new List<ML.TrackingManager.TrackingManager>();

            ML.Result result = BL.TrackingManager.TrackingManager.GetOrdersPerOutboundShipment(outboundShipment, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            trackingManagertList = (List<ML.TrackingManager.TrackingManager>)result.Object;

            ViewBag.FechaSeleccionada = outboundShipment.fechaSeleccionada;

            return View("Ruta", trackingManagertList);
        }

        [HttpPost]
        public IActionResult GetDetail(string ord_rel)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.TrackingManager.TrackingManager.GetDetail(ord_rel, mode);
            if (!result.Correct)
            {
                //ViewBag.Error = result.Message;
                return PartialView("_DetalleError", result.Message);
            }
            var olpnList = (List<string>)result.Object;

            return PartialView("_DetalleModal", olpnList);
        }

        //GetReturnedOrders
        [HttpGet]
        public IActionResult ReturnedOrders()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }
            List<ML.TrackingManager.TrackingManager> trackingManagertList = new List<ML.TrackingManager.TrackingManager>();

            ML.Result result = BL.TrackingManager.TrackingManager.GetReturnedOrders(cod_pto, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            trackingManagertList = (List<ML.TrackingManager.TrackingManager>)result.Object;

            return View("ReturnedOrders", trackingManagertList);
        }
    }
}
