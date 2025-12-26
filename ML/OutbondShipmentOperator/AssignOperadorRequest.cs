using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.OutbondShipmentOperator
{
    public class AssignOperadorRequest
    {
        public ML.OutbondShipmentOperator.OutboundShipment OutbondShipment { get; set; }
        public ML.OutbondShipmentOperator.Operator Ope { get; set; }
    }
}
