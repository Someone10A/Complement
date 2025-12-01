using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ML.DeliveryPlanner;
using ML.Maintenance;


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

        [HttpPost]
        public IActionResult SendToRoute([FromBody] PlanSchema planSchema)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.ChangeInRoute(planSchema, mode);

            return Json(result);
        }


        [HttpGet]
        public IActionResult Plans()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }
            List<ML.DeliveryPlanner.Schema> schemaList = new List<ML.DeliveryPlanner.Schema>();

            ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.GetSchemas(mode);
            if(!result.Correct)
            {
                return View("Plans", (result.Correct, schemaList));
            }
            schemaList = (List<ML.DeliveryPlanner.Schema>)result.Object;

            return View("Plans", (result.Correct, schemaList));
        }


        [HttpPost]
        public IActionResult CreateRouting([FromBody] ML.DeliveryPlanner.Schema schema)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }
            List<ML.DeliveryPlanner.Schema> schemaList = new List<ML.DeliveryPlanner.Schema>();

            ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.CreateRouting(schema,mode);

            return Json(result);
        }

        public IActionResult GetRoutesBySchema([FromBody] ML.DeliveryPlanner.Schema schema)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.GetRoutesBySchema(schema, mode);

            if (!result.Correct)
            {
                return BadRequest(new
                {
                    correct = false,
                    message = result.Message
                });
            }

            var routingDetailList = (List<ML.DeliveryPlanner.RoutingDetail>)result.Object;

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add($"Plan_{schema.PreFol}");

            ws.Cell(1, 1).Value = "Folio Raíz";
            ws.Cell(1, 2).Value = "Folio Header";
            ws.Cell(1, 3).Value = "Almacén";
            ws.Cell(1, 4).Value = "SCN";
            ws.Cell(1, 5).Value = "Orden Rel.";
            ws.Cell(1, 6).Value = "Fecha Entrega";
            ws.Cell(1, 7).Value = "Tipo Entrega";
            ws.Cell(1, 8).Value = "División Entrega";
            ws.Cell(1, 9).Value = "Familia";
            ws.Cell(1, 10).Value = "Estado";
            ws.Cell(1, 11).Value = "Municipio";
            ws.Cell(1, 12).Value = "Sector";
            ws.Cell(1, 13).Value = "Código Postal";
            ws.Cell(1, 14).Value = "Colonia";
            ws.Cell(1, 15).Value = "Panel";
            ws.Cell(1, 16).Value = "Volado";
            ws.Cell(1, 17).Value = "Más General";
            ws.Cell(1, 18).Value = "Longitud";
            ws.Cell(1, 19).Value = "Latitud";

            int row = 2;

            if (routingDetailList?.Count > 0)
            {
                foreach (var r in routingDetailList)
                {
                    ws.Cell(row, 1).Value = r.PreFolRaiz;
                    ws.Cell(row, 2).Value = r.PreFolHeader;
                    ws.Cell(row, 3).Value = r.PtoAlm;
                    ws.Cell(row, 4).Value = r.NumScn;
                    ws.Cell(row, 5).Value = r.OrdRel;
                    ws.Cell(row, 6).Value = r.FecEnt;
                    ws.Cell(row, 7).Value = r.TipEnt;
                    ws.Cell(row, 8).Value = r.DivEnt;
                    ws.Cell(row, 9).Value = r.DesFam;
                    ws.Cell(row, 10).Value = r.EdoCli;
                    ws.Cell(row, 11).Value = r.MunCli;
                    ws.Cell(row, 12).Value = r.Sector;
                    ws.Cell(row, 13).Value = r.CodPos;
                    ws.Cell(row, 14).Value = r.ColCli;
                    ws.Cell(row, 15).Value = r.Panel;
                    ws.Cell(row, 16).Value = r.Volado;
                    ws.Cell(row, 17).Value = r.MasGen;
                    ws.Cell(row, 18).Value = r.Longitud;
                    ws.Cell(row, 19).Value = r.Latitud;

                    row++;
                }
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"Planeacion_{schema.PreFol}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }


        [HttpPost]
        public IActionResult OverwriteRouting(IFormFile excel, string schema)
        {
            if (excel == null || excel.Length == 0)
            {
                return Json(new { correct = false, message = "No se recibió archivo." });
            }

            ML.DeliveryPlanner.Schema schemaObj =
                Newtonsoft.Json.JsonConvert.DeserializeObject<ML.DeliveryPlanner.Schema>(schema);

            try
            {
                List<ML.DeliveryPlanner.RoutingDetail> routingList = ReadExcelRoutingDetails(excel);

                if (routingList == null || routingList.Count == 0)
                {
                    return Json(new { correct = false, message = "El archivo está vacío o no tiene datos válidos." });
                }

                ML.Result result = BL.DeliveryPlanner.DeliveryPlanner.OverwriteRouting(routingList, schemaObj, mode);

                return Json(new
                {
                    correct = result.Correct,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new { correct = false, message = "Error procesando el archivo: " + ex.Message });
            }
        }

        private List<ML.DeliveryPlanner.RoutingDetail> ReadExcelRoutingDetails(IFormFile excel)
        {
            List<ML.DeliveryPlanner.RoutingDetail> list = new List<ML.DeliveryPlanner.RoutingDetail>();

            using (var stream = new MemoryStream())
            {
                excel.CopyTo(stream);
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    var ws = workbook.Worksheet(1);
                    var rows = ws.RangeUsed().RowsUsed();

                    bool isHeader = true;
                    foreach (var row in rows)
                    {
                        if (isHeader) // saltar encabezado
                        {
                            isHeader = false;
                            continue;
                        }

                        var detail = new ML.DeliveryPlanner.RoutingDetail
                        {
                            PreFolRaiz = row.Cell(1).GetString(),
                            PreFolHeader = row.Cell(2).GetString(),
                            PtoAlm = row.Cell(3).GetString(),
                            NumScn = row.Cell(4).GetString(),
                            OrdRel = row.Cell(5).GetString(),
                            FecEnt = row.Cell(6).GetString(),
                            TipEnt = row.Cell(7).GetString(),
                            DivEnt = row.Cell(8).GetString(),
                            DesFam = row.Cell(9).GetString(),
                            EdoCli = row.Cell(10).GetString(),
                            MunCli = row.Cell(11).GetString(),
                            Sector = row.Cell(12).GetString(),
                            CodPos = row.Cell(13).GetString(),
                            ColCli = row.Cell(14).GetString(),
                            Panel = row.Cell(15).GetString(),
                            Volado = row.Cell(16).GetString(),
                            MasGen = row.Cell(17).GetString(),
                            Longitud = row.Cell(18).GetString(),
                            Latitud = row.Cell(19).GetString()
                        };

                        list.Add(detail);
                    }
                }
            }

            return list;
        }

    }
}
