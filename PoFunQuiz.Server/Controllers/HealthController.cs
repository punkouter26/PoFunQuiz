using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PoFunQuiz.Server.Controllers;

[ApiController]
[Route("/")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;

    public HealthController(ILogger<HealthController> logger, IHttpClientFactory? httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("healthz")]
    public async Task<IActionResult> GetHealthz()
    {
        var diagnosticsBase = "/api/diagnostics";
        var client = _httpClientFactory?.CreateClient() ?? new HttpClient { BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}") };

        var results = new Dictionary<string, object?>();
        var overallHealthy = true;

        async Task<(bool ok, JsonElement? body)> CallEndpoint(string path)
        {
            try
            {
                var resp = await client.GetAsync(path);
                var ok = resp.IsSuccessStatusCode;
                if (!ok) overallHealthy = false;

                string content = string.Empty;
                try
                {
                    content = await resp.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var doc = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return (ok, doc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON from {Path}", path);
                }

                return (ok, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling {Path}", path);
                overallHealthy = false;
                return (false, null);
            }
        }

        // API
        var (apiOk, apiBody) = await CallEndpoint(diagnosticsBase + "/api");
        results["api"] = new { status = apiOk ? "success" : "error", body = apiBody };

        // Internet
        var (internetOk, internetBody) = await CallEndpoint(diagnosticsBase + "/internet");
        results["internet"] = new { status = internetOk ? "success" : "error", body = internetBody };

        // Table storage
        var (tableOk, tableBody) = await CallEndpoint(diagnosticsBase + "/tablestorage");
        results["tablestorage"] = new { status = tableOk ? "success" : "error", body = tableBody };

        // OpenAI
        var (openAiOk, openAiBody) = await CallEndpoint(diagnosticsBase + "/openai");
        results["openai"] = new { status = openAiOk ? "success" : "error", body = openAiBody };

        // Build a summary
        var summary = new
        {
            overall = overallHealthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            details = results
        };

        return overallHealthy ? Ok(summary) : StatusCode(503, summary);
    }
}
