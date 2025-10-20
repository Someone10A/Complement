using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ML.BaseControl
{
    public class OrderResult
    {
        [JsonPropertyName("OrderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("OrderStatus")]
        public string OrderStatus { get; set; }

        [JsonPropertyName("Olpn")]
        public string Olpn { get; set; }

        [JsonPropertyName("Product")]
        public string Product { get; set; }

        [JsonPropertyName("LpnStatus")]
        public string LpnStatus { get; set; }

        [JsonPropertyName("Sku")]
        public string Sku { get; set; }

        [JsonPropertyName("Quantity")]
        public double Quantity { get; set; }
    }
}
