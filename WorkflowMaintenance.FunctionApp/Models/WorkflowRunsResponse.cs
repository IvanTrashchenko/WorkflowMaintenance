using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkflowMaintenance.FunctionApp.Models
{
    public class WorkflowRunsResponse
    {
        [JsonPropertyName("value")]
        public List<WorkflowRunItem> Value { get; set; }
        [JsonPropertyName("nextLink")]
        public string NextLink { get; set; }
    }
}
