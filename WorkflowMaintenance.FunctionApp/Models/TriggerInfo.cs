using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkflowMaintenance.FunctionApp.Models
{
    public class TriggerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("inputsLink")]
        public LinkInfo InputsLink { get; set; }

        [JsonPropertyName("outputsLink")]
        public LinkInfo OutputsLink { get; set; }
    }
}
