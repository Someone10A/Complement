using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.Maintenance
{
    public class ApiRequestWMS
    {
        public bool Successes { get; set; }
        public string? OrderNumber { get; set; }
        public int? IdEstatus { get; set; }
        public string? DescEstatus { get; set; }
        public bool EnvioBloqueado { get; set; }
    }
}
