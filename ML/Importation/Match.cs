using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.Importation
{
    public class Match
    {
        public string Sku { get; set; }
        public int PiezasGtm { get; set; }
        public int PiezasLGA { get; set; }
        public int Diff { get; set; }
    }
}
