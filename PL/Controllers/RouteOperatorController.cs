using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;

namespace PL.Controllers
{
    public class RouteOperatorController : Controller
    {
        string mode = "DEV";

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
                
                // Obtener todas las asignaciones en una sola consulta
                var asignacionesResult = BL.RouteOperator.RouteOperator.GetAllAsignacionesOperadores(mode);
                if (asignacionesResult.Correct && asignacionesResult.Object != null)
                {
                    var asignaciones = (Dictionary<string, ML.RouteOperator.AsignacionOperador>)asignacionesResult.Object;
                    
                    // Guardar asignaciones en ViewData para usar en la vista
                    foreach (var kvp in asignaciones)
                    {
                        ViewData[$"Asignacion_{kvp.Key}"] = kvp.Value;
                    }
                }
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
        public IActionResult AsignarRuta(ML.RouteOperator.AsignacionOperador asignacion)
        {
            
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.RouteOperator.InsertAsignacionOperador(asignacion, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        [HttpGet]
        public IActionResult GetAsignacionOperador(string rfcOpe)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            ML.Result result = BL.RouteOperator.RouteOperator.GetAsignacionOperador(rfcOpe, mode);

            if (result.Correct && result.Object != null)
            {
                var asignacion = (ML.RouteOperator.AsignacionOperador)result.Object;
                string estadoDescripcion = asignacion.estatus switch
                {
                    0 => "Ruta Asignada - Es posible asignar a otro chofer",
                    1 => "Ruta Cerrada - No es posible modificar",
                    2 => "Ruta Abierta - El operador acepto la ruta",
                    3 => "Ruta Finalizada - El operador finalizo la ruta",
                    _ => "Estado desconocido"
                };

                return Json(new
                {
                    success = true,
                    asignacion = new
                    {
                        car_sal = asignacion.car_sal,
                        estatus = asignacion.estatus,
                        estadoDescripcion = estadoDescripcion
                    }
                });
            }

            return Json(new
            {
                success = false,
                message = "No se encontró asignación"
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
    }
}
