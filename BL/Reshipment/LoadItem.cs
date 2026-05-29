using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Reshipment
{
    public class LoadItem
    {
        public long IdCarga { get; set; }
        public string CargaSalida { get; set; }
        public int IdEstatusCarga { get; set; }
        public string SigAlmacen { get; set; }
    }
}
