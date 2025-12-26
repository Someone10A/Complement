using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.Operator
{
    public class RouteRequest
    {
        public ML.Operator.RouteHeader Header { get; set; }
        public ML.Operator.RouteDetail Detail { get; set; }
    }
}
