using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ML.BaseControl
{
    public class ApiResponse
    {
        [JsonPropertyName("result_count")]
        public int ResultCount { get; set; }

        [JsonPropertyName("page_count")]
        public int PageCount { get; set; }

        [JsonPropertyName("page_nbr")]
        public int PageNumber { get; set; }

        [JsonPropertyName("next_page")]
        public string? NextPage { get; set; }

        [JsonPropertyName("previous_page")]
        public string? PreviousPage { get; set; }

        [JsonPropertyName("results")]
        public List<OrderResult> Results { get; set; } = new();
    }
}
