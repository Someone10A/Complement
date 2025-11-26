using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.DeliveryPlanner
{
    public class PlanSchema
    {
        public int routes { get; set;  }
        public List<ML.DeliveryPlanner.PlanInfo> planInfoList { get; set; }
    }
}
