using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkflowMaintenance.FunctionApp.Models
{
    public class WorkflowRunProperties
    {
        [JsonPropertyName("waitEndTime")] public DateTimeOffset? WaitEndTime { get; set; }
        [JsonPropertyName("startTime")] public DateTimeOffset StartTime { get; set; }
        [JsonPropertyName("endTime")] public DateTimeOffset? EndTime { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("correlation")] public CorrelationInfo Correlation { get; set; }
        [JsonPropertyName("workflow")] public WorkflowVersionInfo Workflow { get; set; }
        [JsonPropertyName("trigger")] public TriggerInfo Trigger { get; set; }
    }
}
