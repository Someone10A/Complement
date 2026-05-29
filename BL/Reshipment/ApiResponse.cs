using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Reshipment
{
    public class ApiResponse
    {
        public int result_count { get; set; }
        public int page_count { get; set; }
        public int page_nbr { get; set; }
        public string next_page { get; set; }
        public string previous_page { get; set; }
        public List<LoadItem> results { get; set; }
    }
}
