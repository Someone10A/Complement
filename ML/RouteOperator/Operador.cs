using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.RouteOperator
{
    public class Operador
    {
        public decimal cod_emp { get; set; }
        public string? rfc_ope { get; set; }
        public string nom_ope { get; set; } = "";
        public string active { get; set; } = "0";
        public string password { get; set; } = "";
    }
}

