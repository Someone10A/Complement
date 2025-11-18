using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.ReSender
{
    public class Order
    {
        public string Orden { get; set; }
        public List<ML.ReSender.Header> Headers { get; set; }
    }
}
