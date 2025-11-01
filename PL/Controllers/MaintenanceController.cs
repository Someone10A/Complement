using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using ML.Maintenance;

namespace PL.Controllers
{
    public class MaintenanceController : Controller
    {
        string mode = "DEV";
        string cod_pto = "870";

        public class UpdateScnRequest
        {
            public ML.Maintenance.ConfirmedInfoByScn? ConfirmedInfo { get; set; }
            public ML.Maintenance.InfoByScn? OriginalInfo { get; set; }
        }

        [HttpGet]
        public IActionResult InfoByScn()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View("Maintenance");
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

            // Verificar si existe registro en ora_mantenimiento
            var checkResult = BL.Maintenance.Maintenance.CheckOraMantenimiento(infoByScn.CodPto, infoByScn.NumEdc, numScn, mode);
            bool oraMantenimientoExists = false;
            if (checkResult.Correct && checkResult.Object is bool exists)
            {
                oraMantenimientoExists = exists;
            }
            ViewBag.OraMantenimientoExists = oraMantenimientoExists;

            return View("Maintenance", infoByScn); 
        }

        [HttpPost]
        [Consumes("application/json")]
        public IActionResult UpdateScnInfo([FromBody] UpdateScnRequest request)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return Unauthorized();
            }

            // Validar que se recibió el objeto
            if (request?.ConfirmedInfo == null || string.IsNullOrEmpty(request.ConfirmedInfo.NumScn))
            {
                TempData["ErrorMessage"] = "Error: Información incompleta recibida";
                return BadRequest(new { success = false, message = "Información incompleta" });
            }

            var confirmedInfo = request.ConfirmedInfo;
            var originalInfo = request.OriginalInfo ?? new ML.Maintenance.InfoByScn();

            // Asegurar que todos los campos tengan valores (strings vacíos en lugar de null)
            // Campos principales
            confirmedInfo.NumScn = confirmedInfo.NumScn ?? originalInfo.NumScn ?? "";
            confirmedInfo.PtoAlm = confirmedInfo.PtoAlm ?? originalInfo.PtoAlm ?? "";
            confirmedInfo.CodPto = confirmedInfo.CodPto ?? originalInfo.CodPto ?? "";
            confirmedInfo.NumEdc = confirmedInfo.NumEdc ?? originalInfo.NumEdc ?? "";
            confirmedInfo.CodCli = confirmedInfo.CodCli ?? originalInfo.CodCli ?? "";
            confirmedInfo.CodDir = confirmedInfo.CodDir ?? originalInfo.CodDir ?? "";
            
            // Campos de dirección - usar valores enviados si existen, sino mantener originales
            confirmedInfo.Calle = confirmedInfo.Calle ?? originalInfo.Calle ?? "";
            confirmedInfo.NumExt = confirmedInfo.NumExt ?? originalInfo.NumExt ?? "";
            confirmedInfo.NumInt = confirmedInfo.NumInt ?? originalInfo.NumInt ?? "";
            confirmedInfo.Colonia = confirmedInfo.Colonia ?? originalInfo.Colonia ?? "";
            confirmedInfo.Municipio = confirmedInfo.Municipio ?? originalInfo.Municipio ?? "";
            confirmedInfo.Estado = confirmedInfo.Estado ?? originalInfo.Estado ?? "";
            confirmedInfo.CodPos = confirmedInfo.CodPos ?? originalInfo.CodPos ?? "";
            confirmedInfo.Referencias = confirmedInfo.Referencias ?? originalInfo.Referencias ?? "";
            confirmedInfo.Observaciones = confirmedInfo.Observaciones ?? originalInfo.Observaciones ?? "";
            confirmedInfo.Longitud = confirmedInfo.Longitud ?? originalInfo.Longitud ?? "";
            confirmedInfo.Latitud = confirmedInfo.Latitud ?? originalInfo.Latitud ?? "";
            
            // Campos adicionales
            confirmedInfo.Panel = confirmedInfo.Panel ?? originalInfo.Panel ?? "";
            confirmedInfo.Volado = confirmedInfo.Volado ?? originalInfo.Volado ?? "";
            confirmedInfo.MasGen = confirmedInfo.MasGen ?? originalInfo.MasGen ?? "";
            
            // Fecha - mantener la enviada o la original
            confirmedInfo.FecEnt = confirmedInfo.FecEnt ?? originalInfo.FecEnt ?? "";
            
            // Información de usuario
            confirmedInfo.UsuCon = usuId ?? "";
            confirmedInfo.RolUsu = confirmedInfo.RolUsu ?? HttpContext.Session.GetString("rol_usu") ?? "";

            // Determinar si es posfechado basado en cambio de fecha
            confirmedInfo.IsPosfec = !string.IsNullOrEmpty(confirmedInfo.FecEnt) && 
                                   !confirmedInfo.FecEnt.Equals(originalInfo.FecEnt ?? "", StringComparison.OrdinalIgnoreCase);

            ML.Result result = BL.Maintenance.Maintenance.UpdateScnInfo(confirmedInfo, originalInfo, mode, usuId);

            if (result.Correct)
            {
                TempData["SuccessMessage"] = "Información actualizada correctamente";
                return Ok(new { success = true, message = "Información actualizada correctamente" });
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return BadRequest(new { success = false, message = result.Message });
            }
        }
    }
}
