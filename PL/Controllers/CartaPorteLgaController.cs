using Microsoft.AspNetCore.Mvc;
using ML.CartaPorteLga;

namespace PL.Controllers
{
    public class CartaPorteLgaController : Controller
    {
        string mode = "PRO";

        [HttpGet]
        public IActionResult Index()
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            return View();
        }

        [HttpGet]
        public IActionResult AsignarUnidad(string folio)
        {
            var usuId = HttpContext.Session.GetString("usu_id");
            if (string.IsNullOrEmpty(usuId))
            {
                return RedirectToAction("Login", "Login");
            }

            ViewBag.Folio = folio ?? "";
            return View();
        }
    }

    [ApiController]
    [Route("api")]
    public class CartaPorteLgaApiController : ControllerBase
    {
        string mode = "PRO";

        [HttpGet("precon/{precon}")]
        public async Task<IActionResult> GetScnByCono(string precon)
        {
            try
            {
                var result = await BL.CartaPorteLga.CartaPorteLga.GetScnByCono(precon, mode);
                
                if (!result.Correct)
                {
                    return BadRequest(new
                    {
                        type = "Response",
                        title = "Bad Request",
                        status = 400,
                        traceId = precon,
                        errors = new
                        {
                            message = new[] { result.Message }
                        }
                    });
                }
                
                return Ok(result.Object);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        [HttpGet("operadoresLga")]
        public async Task<IActionResult> GetOperadores()
        {
            try
            {
                var result = await BL.CartaPorteLga.CartaPorteLga.GetOperadores(mode);
                
                if (!result.Correct)
                {
                    return NotFound(result.Message);
                }
                
                return Ok(result.Object);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        [HttpGet("unidadesLga")]
        public async Task<IActionResult> GetUnidades()
        {
            try
            {
                var result = await BL.CartaPorteLga.CartaPorteLga.GetUnidades(mode);
                
                if (!result.Correct)
                {
                    return NotFound(result.Message);
                }
                
                return Ok(result.Object);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        [HttpPost("enviarCarta")]
        public async Task<IActionResult> EnviarCarta([FromBody] EnviarCartaRequest request)
        {
            try
            {
                System.Console.WriteLine($"\n[ENVIAR CARTA] Iniciando proceso para folio: {request?.Folio}, FechaSalida: {request.FechaSalida}");
                
                if (request == null || string.IsNullOrEmpty(request.Folio))
                {
                    System.Console.WriteLine("[ENVIAR CARTA] Error: Request inválido o folio faltante");
                    return BadRequest(new { 
                        type = "Response", 
                        title = "Bad Request", 
                        status = 400, 
                        traceId = request?.Folio ?? "unknown", 
                        errors = new { 
                            message = new[] { "Request inválido o folio faltante" } 
                        } 
                    });
                }

                var result = await BL.CartaPorteLga.CartaPorteLga.EnviarCarta(request, mode);
                
                if (!result.Correct)
                {
                    return BadRequest(new { 
                        type = "Response", 
                        title = "Bad Request", 
                        status = 400, 
                        traceId = request.Folio, 
                        errors = new { 
                            message = new[] { result.Message } 
                        } 
                    });
                }
                
                return Ok(result.Object);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ENVIAR CARTA] Error: {ex.Message}");
                System.Console.WriteLine($"[ENVIAR CARTA] Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
