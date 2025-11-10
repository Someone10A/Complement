using Microsoft.AspNetCore.Mvc;
using System.Data.Odbc;

namespace PL.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View(new ML.Login.Login());
        }
        [HttpPost]
        public ActionResult Login(ML.Login.Login login)
        {
            string mode = "PRO";

            ML.Result result = BL.Login.Login.Log(login,mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
                return View(login);
            }
            ML.Login.Login loged = (ML.Login.Login)result.Object;

            HttpContext.Session.SetString("usu_id", loged.usu_id ?? "");
            HttpContext.Session.SetString("usu_nombre", loged.usu_nombre ?? "");
            HttpContext.Session.SetString("cv_area", loged.cv_area ?? "");
            HttpContext.Session.SetString("nombre", loged.nombre ?? "");
            HttpContext.Session.SetString("sub_rol", loged.sub_rol ?? "");
            HttpContext.Session.SetString("pto_alm", loged.pto_alm ?? "");

            return RedirectToAction("Index", "Home");

            //if (loged.cv_area == "SIS" || loged.sub_rol == "SIS")
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            //else if (loged.cv_area == "CIC" && loged.sub_rol == "CIC")
            //{
            //    return RedirectToAction("GetOrders", "Importations");
            //}
            //else if (loged.cv_area == "GGB" || loged.cv_area == "SAC" || loged.cv_area == "SAC")
            //else if (loged.cv_area == "GGB" || loged.cv_area == "CON")
            //        {
            //            return RedirectToAction("GetTrackingPerDay", "TrackingManager");
            //        }
            //        else
            //        {
            //            ViewBag.Error = "Área no autorizada.";
            //            return View();
            //        }
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Login");
        }

        public static List<string> GetBaseUsers(string mode)
        {
            return BL.Login.Login.GetBaseUsers(mode);
        }

        public static List<string> GetInternetUsers(string mode)
        {
            return BL.Login.Login.GetInternetUsers(mode);
        }

        public static List<string> GetSupervisorUsers(string mode)
        {
            return BL.Login.Login.GetSupervisorUsers(mode);
        }
    }
}
