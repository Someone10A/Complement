using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class OutbondShipmentOperatorController : Controller
    {
        string mode = "DEV";
        string ptoAlm = "870";
        //Operator
        [HttpGet]
        public IActionResult Operadores()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("Operator");
        }   
        [HttpGet]
        public IActionResult GetOperadores()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.OutbondShipmentOperator.Operator.GetOperadores(mode);
            if (!result.Correct)
            {
                return BadRequest(result.Message);
            }

            return Json(result.Object);
        }
        [HttpPatch]
        public IActionResult UpdateStatus(string rfcOpe, string active)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.OutbondShipmentOperator.Operator.UpdateStatus(rfcOpe, active, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        [HttpPost]
        public IActionResult AddOperador([FromBody] ML.OutbondShipmentOperator.Operator ope)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.OutbondShipmentOperator.Operator.AddOperador(ope, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        [HttpPatch]
        public IActionResult ResetPassword(string rfcOpe, string newPassword)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.OutbondShipmentOperator.Operator.ResetPassword(rfcOpe, newPassword, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        //Route
        [HttpGet]
        public IActionResult OutbondShipment()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("OutbondShipment");
        }
        [HttpGet]
        public IActionResult GetOutbondShipmentsQualified()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.OutbondShipmentOperator.OutbondShipment.GetOutbondShipmentsQualified(ptoAlm, mode);

            return Json(new
            {
                success = result.Correct,
                outbondShipmentList = result.Object,
                message = result.Message
            });
        }
        [HttpPost]
        public IActionResult InsertAssign([FromBody] ML.OutbondShipmentOperator.AssignOperadorRequest assignOperadorRequest)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.OutbondShipmentOperator.OutbondShipment.InsertAssign(assignOperadorRequest.OutbondShipment, assignOperadorRequest.Ope, mode);

            return Json(new
            {
                success = result.Correct,
                pendingOrders = result.Object,
                message = result.Message
            });
        }
        [HttpDelete]
        public IActionResult DeleteAssign([FromBody] ML.OutbondShipmentOperator.OutboundShipment outbondShipment)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.OutbondShipmentOperator.OutbondShipment.DeleteAssign(outbondShipment, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }
        [HttpGet]
        public IActionResult GetOperadoresActive()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.OutbondShipmentOperator.OutbondShipment.GetOperadoresActive(mode);
            if (!result.Correct)
            {
                return BadRequest(new { message = result.Message });
            }

            return Json(result.Object);
        }
    }
}
