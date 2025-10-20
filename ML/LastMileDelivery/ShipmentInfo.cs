using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.LastMileDelivery
{
    public class ShipmentInfo
    {
        public string num_scn {  get; set; }
        public string ord_rel_wms {  get; set; }
        public string fec_car_wms { get; set; }
        public string car_sal_wms { get; set; }
        public string ord_rel_lga {  get; set; }
        public string fec_car_lga { get; set; }
        public string car_sal_lga { get; set; }
    }
}
