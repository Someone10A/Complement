using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.Importation
{
    public class ImportationMatch
    {
        public string Key { get; set; } = "sku";
        public string Pivote { get; set; } = "GTM";
        public string FolGtm { get; set; }    
        public string OcMadre { get; set; }    
        public string OcHija { get; set; }    
    }
}
