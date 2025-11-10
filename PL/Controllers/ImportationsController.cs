using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace PL.Controllers
{
    public class ImportationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        string mode = "PRO";

        [HttpGet]
        public ActionResult GetOrders()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.Importation.ora_imp_corden> orders = new List<ML.Importation.ora_imp_corden>();

            ML.Result result = BL.Importation.ImporationSplit.GetOrders(mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            else
            {
                orders = (List<ML.Importation.ora_imp_corden>)result.Object;
            }
 
            return View("Orders",orders);
        }

        [HttpGet]
        public ActionResult GetDerivedOrders(string order, string fol_gtm, string total_piezas)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ViewData["FolioGtm"] = fol_gtm;
            ViewData["PiezasMadre"] = total_piezas;

            List<ML.Importation.OrderDerived> orders = new List<ML.Importation.OrderDerived>();

            ML.Result result = BL.Importation.ImporationSplit.GetDerivedOrders(order,mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            orders = (List<ML.Importation.OrderDerived>)result.Object;
 
            return View("Relacionadas",orders);
        }

        [HttpPost]
        public IActionResult InsertSplit(ML.Importation.ora_imp_divide divide)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Json(new { success = false, message = "Sesión expirada." });
            }

            ML.Result result = BL.Importation.ImporationSplit.InsertSplit(divide, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        [HttpGet]
        public IActionResult GetRelation()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("GetRelation");
        }
        [HttpPost]
        public ActionResult GetRelation(string orderList)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.Importation.OrderDerived> orders = new List<ML.Importation.OrderDerived>();

            ML.Result result = BL.Importation.ImporationSplit.GetRelation(orderList, mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            orders = (List<ML.Importation.OrderDerived>)result.Object;

            return Json(orders);
        }

        [HttpGet]
        public ActionResult MatchOrders()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            var mc = new ML.Importation.MatchControl();
            return View("MatchOrders", mc);
        }

        [HttpPost]
        public ActionResult MatchOrders(ML.Importation.ImportationMatch importationMatch)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result resultGetMatchOrders = BL.Importation.ImportationMatch.GetMatchOrders(importationMatch, mode);
            if (!resultGetMatchOrders.Correct)
            {
                ViewBag.Errors = resultGetMatchOrders.Message;
            }
            ML.Importation.MatchControl mc = new ML.Importation.MatchControl();
            mc.Filtros = importationMatch;
            mc.ListaMaches = (List<ML.Importation.Match>)resultGetMatchOrders.Object;

            //return View("MatchOrders", matchList);
            return View("MatchOrders",mc);
        }
    }
}
