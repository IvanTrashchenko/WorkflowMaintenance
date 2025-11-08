using WorkflowMaintenance.FunctionApp.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http.Headers;
using System.Text.Json;
using WorkflowMaintenance.FunctionApp.Helpers;

namespace WorkflowMaintenance.FunctionApp;

public class MaintenanceFunctions
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly NextLinkCheckpoint _checkpoint;

    private const int DelaySec = 60;

    private readonly string SubscriptionId = Environment.GetEnvironmentVariable("SubscriptionId");
    private readonly string ResourceGroup = Environment.GetEnvironmentVariable("ResourceGroup");
    private readonly string LogicAppName = Environment.GetEnvironmentVariable("LogicAppName");
    private readonly string WorkflowName = Environment.GetEnvironmentVariable("WorkflowName");
    private readonly string ApiVersion = Environment.GetEnvironmentVariable("ApiVersion") ?? "2018-11-01";
    // You need to have an Azure AD App Registration with appropriate permissions (Logic Apps Standard Operator) & a Client Secret
    private readonly string TenantId = Environment.GetEnvironmentVariable("TenantId");
    private readonly string ClientId = Environment.GetEnvironmentVariable("ClientId");
    private readonly string ClientSecret = Environment.GetEnvironmentVariable("ClientSecret");

    private string _token = null;

    private int totalRunningRunsCanceled = 0;

    private string initialListUrl;
    public MaintenanceFunctions(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MaintenanceFunctions>();
        _http = new HttpClient();

        _checkpoint = new NextLinkCheckpoint(_logger);

        initialListUrl = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.Web/sites/{LogicAppName}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{WorkflowName}/runs?api-version={ApiVersion}";
    }

    [Function("CancelRunningWorkflowRuns")]
    public async Task CancelRunningWorkflowRuns([TimerTrigger("0 */3 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("CancelRunningWorkflowRuns triggered at: {executionTime}", DateTime.Now);

        await EnsureAuthAsync();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var savedLink = await _checkpoint.ReadNextLinkAsync();
        var listUrl = string.IsNullOrWhiteSpace(savedLink)
            ? initialListUrl
            : savedLink;

        await CancelRunningWorkflowRunsAsync(listUrl);

        _logger.LogInformation("CancelRunningWorkflowRuns execution completed.");
    }

    private async Task CancelRunningWorkflowRunsAsync(string listUrl)
    {
        _logger.LogInformation($"Listing runs from: {listUrl}");
        var listResponse = await _http.GetAsync(listUrl);
        if (!listResponse.IsSuccessStatusCode)
        {
            _logger.LogError($"Error listing runs: {listResponse.StatusCode} / {await listResponse.Content.ReadAsStringAsync()}");
            return;
        }

        var json = await listResponse.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var response = JsonSerializer.Deserialize<WorkflowRunsResponse>(json, options);

        if (response?.Value != null)
        {
            _logger.LogInformation($"Retrieved page with {response.Value.Count} runs.");
            var runningRuns = response.Value
                .Where(wr => string.Equals(wr.Properties.Status, "Running", StringComparison.OrdinalIgnoreCase))
                .ToList();
            _logger.LogInformation($"Found {runningRuns.Count} runs with status 'Running' in this page.");

            var tasks = runningRuns.Select(run => PostCancelAsync(run.Name)).ToList();
            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                _logger.LogInformation($"RunId: {result.RunId} — Cancel response: {(int)result.Response.StatusCode} {result.Response.StatusCode}");
                if (result.Response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning($"Throttle – received 429 for run {result.RunId}. Waiting {DelaySec} seconds.");
                    await Task.Delay(DelaySec * 1000);
                    return;
                }
                else if (result.Response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    totalRunningRunsCanceled++;
                }
            }
        }
        else
        {
            _logger.LogInformation("No runs found in this page.");
        }

        _logger.LogInformation($"Amount of running runs canceled: {totalRunningRunsCanceled}.");

        if (!string.IsNullOrWhiteSpace(response.NextLink))
        {
            _logger.LogInformation("NextLink found — proceeding to next page.");
            await _checkpoint.SaveNextLinkAsync(response.NextLink);
            await CancelRunningWorkflowRunsAsync(response.NextLink);
        }
        else
        {
            _checkpoint.ClearCheckpoint();
            _logger.LogInformation("No NextLink; paging complete.");
        }
    }

    private async Task<(string RunId, HttpResponseMessage Response)> PostCancelAsync(string runId)
    {
        string cancelUrl =
            $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/providers/Microsoft.Web/sites/{LogicAppName}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{WorkflowName}/runs/{runId}/cancel?api-version={ApiVersion}";
        var resp = await _http.PostAsync(cancelUrl, null);
        return (runId, resp);
    }

    private async Task EnsureAuthAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            var url = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["scope"] = "https://management.azure.com/.default",
                ["grant_type"] = "client_credentials"
            });
            var resp = await _http.PostAsync(url, content);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _token = doc.RootElement.GetProperty("access_token").GetString();
            _logger.LogInformation("Retrieved access token.");
        }
    }
}