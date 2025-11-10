using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.CartaPorte
{
    public class EnviarCartaRequest
    {
        public string Folio { get; set; }
        public DateTime FechaSalida { get; set; }
        public string Operador { get; set; }
        public string Unidad { get; set; }
    }
}
