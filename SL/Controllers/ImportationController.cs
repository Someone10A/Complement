using Microsoft.AspNetCore.Mvc;

namespace SL.Controllers
{
    public class ImportationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        [Route("api/Importation/GetOrders")]
        [HttpGet]
        public IActionResult GetOrders(string mode)
        {
            ML.Result result = BL.Importation.ImporationSplit.GetOrders(mode);

            if (result.Correct)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result.Message);
            }
        }

        [Route("api/Importation/GetDerivedOrders")]
        [HttpGet]
        public IActionResult GetDerivedOrders(string order, string mode)
        {
            ML.Result result = BL.Importation.ImporationSplit.GetDerivedOrders(order, mode);

            if (result.Correct)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result.Message);
            }
        }

        [Route("api/Importation/InsertSplit")]
        [HttpPost]
        public IActionResult InsertSplit(ML.Importation.ora_imp_divide divide, string mode)
        {
            ML.Result result = BL.Importation.ImporationSplit.InsertSplit(divide, mode);

            if (result.Correct)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result.Message);
            }
        }
    }
}
