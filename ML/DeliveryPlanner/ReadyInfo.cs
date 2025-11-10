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
        public string ProCli {get; set; }
        public string PobCli {get; set; }
        public string Sector {get; set; }
        public string CpCli {get; set; }
        public string Panel {get; set; }
        public string Volado {get; set; }
        public string MasGen {get; set; }
    }
}
