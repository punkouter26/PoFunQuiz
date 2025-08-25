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
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        
        var client = _httpClientFactory?.CreateClient() ?? new HttpClient(handler);
        
        // Use hardcoded URIs for local testing
        var baseUri = "https://localhost:5001";
        var apiUri = $"{baseUri}/api/diagnostics/api";
        var internetUri = $"{baseUri}/api/diagnostics/internet";
        var tableStorageUri = $"{baseUri}/api/diagnostics/tablestorage";
        var openAiUri = $"{baseUri}/api/diagnostics/openai";
        
        // Remove base address since we're using absolute URIs
        client.BaseAddress = null;

        var results = new Dictionary<string, object?>();
        var overallHealthy = true;

        async Task<(bool ok, JsonElement? body)> CallEndpoint(string path)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, path);
                var resp = await client.SendAsync(request);
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
        var (apiOk, apiBody) = await CallEndpoint(apiUri);
        results["api"] = new { status = apiOk ? "success" : "error", body = apiBody };

        // Internet
        var (internetOk, internetBody) = await CallEndpoint(internetUri);
        results["internet"] = new { status = internetOk ? "success" : "error", body = internetBody };

        // Table storage
        var (tableOk, tableBody) = await CallEndpoint(tableStorageUri);
        results["tablestorage"] = new { status = tableOk ? "success" : "error", body = tableBody };

        // OpenAI
        var (openAiOk, openAiBody) = await CallEndpoint(openAiUri);
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
