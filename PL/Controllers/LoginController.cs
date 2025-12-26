using Microsoft.AspNetCore.Mvc;
using System.Data.Odbc;

namespace PL.Controllers
{
    public class LoginController : Controller
    {
        string mode = "DEV";
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
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Login");
        }
    }
}
