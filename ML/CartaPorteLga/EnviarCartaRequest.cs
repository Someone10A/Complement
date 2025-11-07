using System;

namespace ML.CartaPorteLga
{
    public class EnviarCartaRequest
    {
        public string Folio { get; set; }
        public DateTime FechaSalida { get; set; }
        public string Operador { get; set; }
        public string Unidad { get; set; }
    }
}

