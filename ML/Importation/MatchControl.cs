using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.Importation
{
    public class MatchControl
    {
        public ML.Importation.ImportationMatch Filtros { get; set; } = new ML.Importation.ImportationMatch();
        public List<ML.Importation.Match> ListaMaches { get; set; }
    }
}
