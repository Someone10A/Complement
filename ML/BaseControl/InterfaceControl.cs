using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.BaseControl
{
    public class InterfaceControl
    {
        public string Letra { get; } = "C";
        public string GS { get; } = "GS";
        public string OrdRel { get; set; }
        public string User { get; set; }
        public string Zona { get; } = "Mexico/General";
        public string Uso { get; } = "-06:00";
        public string Reason { get; set; }
        public string OrderType { get; } = "GS.ENTREGA_VENTA_BT";
        public string Nulo0 { get; }
        public string Nulo1 { get; }
        public string Nulo2 { get; }
        public string Cierre { get; }
    }
}
