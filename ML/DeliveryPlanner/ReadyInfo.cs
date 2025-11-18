using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.DeliveryPlanner
{
    public class ReadyInfo
    {
        public bool IsReady { get; set; }
        public string PtoAlm { get; set; }
        public string NumScn { get; set; }
        public string WmsEst { get; set; }
        public string EdoCli {get; set; }
        public string MunCli {get; set; }
        public string Sector {get; set; }
        public string CpCli {get; set; }
        public string Panel {get; set; }
        public string Volado {get; set; }
        public string MasGen {get; set; }
        public string TipEnt {get; set; }
        public string OrdRel {get; set; }
        public string FecEnt {get; set; }
    }
}
