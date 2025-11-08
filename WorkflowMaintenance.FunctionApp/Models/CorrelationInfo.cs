using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkflowMaintenance.FunctionApp.Models
{
    public class CorrelationInfo
    {
        [JsonPropertyName("clientTrackingId")]
        public string ClientTrackingId { get; set; }
    }
}
