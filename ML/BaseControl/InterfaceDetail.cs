using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.BaseControl
{
    public class InterfaceDetail
    {
        public string Letra { get; } = "D";
        public string Olpn { get; set; }
        public string Sku { get; set; }
        public string Cantidad { get; set; }
        public string Moneda { get; } // = "MXN";
        public string Ean { get; } //= "EAN";
        public string EanValue { get; }
        public string Cantidad2 { get; set; }//
        public string Reason { get; set; } //= "ENTREGA_OK";
        public string Shipment { get; set; }
        public string GS { get; } = "GS";
        public string Cierre { get; }
    }
}
