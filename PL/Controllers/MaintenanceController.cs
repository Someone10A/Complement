using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using ML.Maintenance;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PL.Controllers
{
    public class MaintenanceController : Controller
    {
        string mode = "DEV";
        string cod_pto = "870";

        [HttpGet]
        public IActionResult InfoByScn()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("Maintenance", new ML.Maintenance.InfoByScn());
        }

        [HttpPost]
        public IActionResult GetScnInfo(string numScn)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            var result = BL.Maintenance.Maintenance.GetScnInfo(numScn, mode);

            ML.Maintenance.InfoByScn infoByScn = new ML.Maintenance.InfoByScn();
            if (!result.Correct)
            {
                return BadRequest($@"{result.Message}");
            }
            infoByScn = (ML.Maintenance.InfoByScn)result.Object;

            //return View("Maintenance", infoByScn); 
            return Json(infoByScn); 
        }

        [HttpPost]
        public IActionResult UpdateScnInfo(ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            confirmedInfo.UsuCon = HttpContext.Session.GetString("usu_id");
            confirmedInfo.RolUsu = HttpContext.Session.GetString("sub_rol");

            ML.Result result = BL.Maintenance.Maintenance.UpdateScnInfo(confirmedInfo, mode);

            return Json(new
            {
                success = result.Correct,
                message = result.Message
            });
        }

        [HttpPost]
        public IActionResult GetTope(string date)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
                return Unauthorized();

            ML.Result result = BL.Maintenance.Maintenance.GetTope(date, "L", mode);

            if (!result.Correct)
            {
                return Json(new
                {
                    success = false,
                    message = result.Message,
                    data = (object)null
                });
            }

            var (ok, mensaje) = ((bool, string))result.Object;

            return Json(new
            {
                success = true,
                message = (string)null,
                data = new
                {
                    isOk = ok,
                    message = mensaje
                }
            });
        }



        [HttpGet]
        public IActionResult GetColByCodPos(string codPos)
        {
            var result = BL.Maintenance.Maintenance.GetColByCodPos(codPos, mode);

            if (result.Correct && result.Object != null)
            {
                var info = (ML.Maintenance.DireccionInfo)result.Object;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        info.Estado,
                        info.Municipio,
                        info.Colonias
                    }
                });
            }

            return Json(new
            {
                success = false,
                message = "No se encontró información para ese código postal."
            });
        }

        //GetToConfirm
        [HttpGet]
        public IActionResult GetToConfirm()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.Maintenance.ScnToConfirm> scnToConfirm = new List<ScnToConfirm>();
            ML.Result result = BL.Maintenance.Maintenance.GetToConfirm(mode);

            ViewBag.Error = result.Message;

            scnToConfirm =(List<ML.Maintenance.ScnToConfirm>)result.Object;

            return View("GetToConfirm", scnToConfirm);
        }
        [HttpGet]
        public IActionResult GetToConfirmExcel()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            List<ML.Maintenance.ScnToConfirm> scnToConfirm = new List<ScnToConfirm>();
            ML.Result result = BL.Maintenance.Maintenance.GetToConfirm(mode);

            ViewBag.Error = result.Message;

            scnToConfirm =(List<ML.Maintenance.ScnToConfirm>)result.Object;

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Embarques");

                ws.Cell(1, 1).Value = "SCN";
                ws.Cell(1, 2).Value = "Tienda";
                ws.Cell(1, 3).Value = "Estado";
                ws.Cell(1, 4).Value = "Fec. Entrega";
                ws.Cell(1, 5).Value = "Orden";
                ws.Cell(1, 6).Value = "Retencion";

                int row = 2;
                if (scnToConfirm.Count > 0) 
                {
                    foreach (ML.Maintenance.ScnToConfirm scn in scnToConfirm)
                    {
                        ws.Cell(row, 1).Value = scn.num_scn;
                        ws.Cell(row, 2).Value = scn.cod_pto ?? "-";
                        ws.Cell(row, 3).Value = scn.estado ?? "-";
                        ws.Cell(row, 4).Value = scn.fec_ent ?? "-";
                        ws.Cell(row, 5).Value = scn.ord_rel ?? "-";
                        ws.Cell(row, 6).Value = scn.is_rete ?? "-";
                        row++;
                    }
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content,
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                $"SCN_Sin_Confirmar_{usuId}_{DateTime.Today.ToString("yyyyMMdd")}.xlsx");
                }
            }


        }
    }
}
