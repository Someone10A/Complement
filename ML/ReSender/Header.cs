using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.ReSender
{
    public class Header
    {
        public string Creacion { get; set; }
        public string Estatus { get; set; }
        public string UltimoEnvio { get; set; }
        public List<ML.ReSender.Detail> Details { get; set; }
    }
}
