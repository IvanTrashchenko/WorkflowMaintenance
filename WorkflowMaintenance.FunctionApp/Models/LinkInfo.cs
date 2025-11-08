using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkflowMaintenance.FunctionApp.Models
{
    public class LinkInfo
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("contentSize")]
        public long? ContentSize { get; set; }
    }
}
