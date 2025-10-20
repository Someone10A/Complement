using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ML.BaseControl
{
    public class Interface
    {
        public ML.BaseControl.InterfaceControl Control { get; set; }
        public ML.BaseControl.InterfaceHeader Header { get; set; }
        public List<ML.BaseControl.InterfaceDetail> Details { get; set; }
    }
}
