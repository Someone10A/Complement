using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.DeliveryPlanner
{
    public class ReadyQuery
    {
        public string PtoAlm { get; set; }//Almacen de entrega
        public string FecEnt { get; set;}//FechaEntrega
        public string TipEnt { get; set;}//Tipo de entrega
    }
}
