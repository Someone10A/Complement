using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.OutbondShipmentOperator
{
    public class Operator
    {
        public string RfcOpe { get; set; }//Maximo 15
        public string NomOpe { get; set; }//Maximo 60
        public string Active { get; set; } 
        public string Password { get; set; }//Maximo 16
    }
}
