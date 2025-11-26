using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.DeliveryPlanner
{
    public class RouteSchema
    {
        public int routeCount {  get; set; }
        public int minOrdersPerRoute { get; set; } = 1;
        public int targetOrdersPerRoute { get; set; } = 15;
        public int maxOrdersPerRoute { get; set; } = 17;
        public string preFol { get; set; }
        public string ptoAlm { get; set; }
        public string tipEnt { get; set; }
        public List<ML.DeliveryPlanner.RouteLines> Orders { get; set; }
    }
}
