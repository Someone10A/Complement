using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class LastMileDeliveryController : Controller
    {
        string mode = "PRO";
        string cod_pto = "870";
        [HttpGet]
        public IActionResult GetShipmentsByDay()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("Shipments");
        }
        [HttpPost]
        public IActionResult GetShipmentsByDay(string date, string actionType)
        {
            string dateGen = DateTime.Now.ToString("yyyyMMddHHmm");
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            var result = BL.LastMileDelivery.LastMileDelivery.GetShipmentByShipment(date, cod_pto, mode);
            if (!result.Correct || result.Object == null)
            {
                return actionType == "Excel"
                    ? BadRequest("No se pudo generar el archivo.")
                    : View("Shipments", new List<ML.LastMileDelivery.ShipmentInfo>());
            }

            var lista = result.Object as List<ML.LastMileDelivery.ShipmentInfo>;

            if (actionType == "Excel")
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Embarques");

                    ws.Cell(1, 1).Value = "SCN";
                    ws.Cell(1, 2).Value = "Orden WMS";
                    ws.Cell(1, 3).Value = "Fec. Carga WMS";
                    ws.Cell(1, 4).Value = "Car. Salida WMS";
                    ws.Cell(1, 5).Value = "Orden Teorica";
                    ws.Cell(1, 6).Value = "Fec. Carga LGA";
                    ws.Cell(1, 7).Value = "Fol. Salida LGA";

                    int row = 2;
                    foreach (var item in lista)
                    {
                        ws.Cell(row, 1).Value = item.num_scn;
                        ws.Cell(row, 2).Value = item.ord_rel_wms ?? "-";
                        ws.Cell(row, 3).Value = item.fec_car_wms ?? "-";
                        ws.Cell(row, 4).Value = item.car_sal_wms ?? "-";
                        ws.Cell(row, 5).Value = item.ord_rel_lga ?? "-";
                        ws.Cell(row, 6).Value = item.fec_car_lga ?? "-";
                        ws.Cell(row, 7).Value = item.car_sal_lga ?? "-";
                        row++;
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content,
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"Embarques_{usuId}_{date}_{dateGen}.xlsx");
                    }
                }
            }

            return View("Shipments", lista);
        }
        
        [HttpGet]
        public IActionResult GetShipmentsByQuery()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("ShipmentsQuery");
        }
        [HttpPost]
        public IActionResult GetShipmentsByQuery(ML.LastMileDelivery.QueryInfo queryInfo, string actionType)
        {
            string dateGen = DateTime.Now.ToString("yyyyMMddHHmm");
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            queryInfo.cod_pto = cod_pto;

            var result = BL.LastMileDelivery.LastMileDelivery.GetShipmentByQuery(queryInfo, mode);
            if (!result.Correct || result.Object == null)
            {
                return actionType == "Excel"
                    ? BadRequest("No se pudo generar el archivo.")
                    : View("ShipmentsQuery", new List<ML.LastMileDelivery.ShipmentInfo>());
            }

            var lista = result.Object as List<ML.LastMileDelivery.ShipmentInfo>;

            if (actionType == "Excel")
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add($@"Embarques{queryInfo.pivotTable}");

                    ws.Cell(1, 1).Value = "SCN";
                    ws.Cell(1, 2).Value = "Orden WMS";
                    ws.Cell(1, 3).Value = "Fec. Carga WMS";
                    ws.Cell(1, 4).Value = "Car. Salida WMS";
                    ws.Cell(1, 5).Value = "Orden Teorica";
                    ws.Cell(1, 6).Value = "Fec. Carga LGA";
                    ws.Cell(1, 7).Value = "Fol. Salida LGA";

                    int row = 2;
                    foreach (var item in lista)
                    {
                        ws.Cell(row, 1).Value = item.num_scn;
                        ws.Cell(row, 2).Value = item.ord_rel_wms ?? "-";
                        ws.Cell(row, 3).Value = item.fec_car_wms ?? "-";
                        ws.Cell(row, 4).Value = item.car_sal_wms ?? "-";
                        ws.Cell(row, 5).Value = item.ord_rel_lga ?? "-";
                        ws.Cell(row, 6).Value = item.fec_car_lga ?? "-";
                        ws.Cell(row, 7).Value = item.car_sal_lga ?? "-";
                        row++;
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content,
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"Embarques_{usuId}_{queryInfo.pivotTable}{queryInfo.fec_pri}_{dateGen}.xlsx");
                    }
                }
            }

            return View("ShipmentsQuery", lista);
        }
    }
}
