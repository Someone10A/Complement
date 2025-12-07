using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;

namespace PL.Controllers
{
    public class RouteOperatorController : Controller
    {
        string mode = "DEV";

        // Vista Operadores
        [HttpGet]
        public IActionResult Index()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return RedirectToAction("ListOperadores");
        }

        [HttpGet]
        public IActionResult ListOperadores()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.RouteOperator.Operador> operadores = new List<ML.RouteOperator.Operador>();

            ML.Result result = BL.RouteOperator.RouteOperator.GetOperadores(mode);
            if (!result.Correct)
            {
                ViewBag.Error = result.Message;
            }
            else
            {
                operadores = (List<ML.RouteOperator.Operador>)result.Object;
            }

            return View(operadores);
        }

        [HttpPost]
        public IActionResult UpdateActiveStatus(decimal codEmp, string rfcOpe, string active)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.RouteOperator.UpdateActiveStatus(codEmp, rfcOpe, active, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult AddOperador(ML.RouteOperator.Operador operador)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.RouteOperator.AddOperador(operador, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult ResetPassword(decimal codEmp, string rfcOpe, string newPassword)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.RouteOperator.ResetPassword(codEmp, rfcOpe, newPassword, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        // Vista Asignar carga de salida a operador
        [HttpPost]
        public IActionResult AsignarRuta(ML.RouteOperator.AsignacionOperador asignacion)
        {
            
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.AsignarCarSal.InsertAsignacionOperador(asignacion, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarAsignacion(string carSal)
        {
            
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.RouteOperator.DeleteAsignacionOperador(carSal, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        [HttpGet]
        public IActionResult GetRutasConScn(string rfcOpe)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(rfcOpe))
            {
                return Json(new
                {
                    success = false,
                    message = "RFC del operador es requerido"
                });
            }

            ML.Result result = BL.RouteOperator.AsignarCarSal.GetRutasConEstatusNoCerrado(rfcOpe, mode);

            return Json(new
            {
                success = result.Correct,
                rutas = result.Object,
                message = result.Message
            });
        }

        [HttpGet]
        public IActionResult ListCarSal()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View(new List<string>());
        }

        [HttpGet]
        public IActionResult GetCargasSalidaVirgenesJson()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.AsignarCarSal.GetCargasSalidaVirgenes(mode);
            if (!result.Correct)
            {
                return Json(new
                {
                    success = false,
                    message = result.Message,
                    cargasSalida = new List<object>()
                });
            }
            else
            {
                var cargasConInfo = result.Object as List<ML.RouteOperator.CargaSalidaConAsignacion>;
                if (cargasConInfo == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Error al procesar los datos",
                        cargasSalida = new List<object>()
                    });
                }

                return Json(new
                {
                    success = true,
                    cargasSalida = cargasConInfo.Select(c => new
                    {
                        carSal = c.CarSal,
                        tieneAsignacion = c.TieneAsignacion,
                        asignacion = c.TieneAsignacion ? new
                        {
                            cod_emp = c.Asignacion.cod_emp,
                            pto_alm = c.Asignacion.pto_alm,
                            car_sal = c.Asignacion.car_sal,
                            rfc_ope = c.Asignacion.rfc_ope,
                            estatus = c.Asignacion.estatus
                        } : null
                    }).ToList()
                });
            }
        }

        [HttpGet]
        public IActionResult GetOperadoresJson()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.RouteOperator.GetOperadores(mode);

            return Json(new
            {
                success = result.Correct,
                operadores = result.Object,
                message = result.Message
            });
        }

        [HttpGet]
        public IActionResult GetAsignacionPorCarSalJson(string carSal)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(carSal))
            {
                return Json(new
                {
                    success = false,
                    message = "Carga de salida es requerida"
                });
            }

            ML.Result result = BL.RouteOperator.AsignarCarSal.GetAsignacionPorCarSal(carSal, mode);

            return Json(new
            {
                success = result.Correct,
                asignacion = result.Object,
                message = result.Message
            });
        }
    }
}
