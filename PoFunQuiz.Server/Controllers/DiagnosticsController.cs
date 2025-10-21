using Microsoft.AspNetCore.Mvc;
using PoFunQuiz.Server.Services;
using PoFunQuiz.Core.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;

namespace PoFunQuiz.Server.Controllers
{
    // This is a diagnostics controller (GoF Design Pattern - Front Controller) that provides health check endpoints
    // for testing connections to external services like Azure OpenAI and Table Storage.
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableStorageSettings _tableStorageSettings;

        public DiagnosticsController(
            IOpenAIService openAIService,
            ILogger<DiagnosticsController> logger,
            TableServiceClient tableServiceClient,
            IOptions<TableStorageSettings> tableStorageSettings)
        {
            _openAIService = openAIService;
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            _tableStorageSettings = tableStorageSettings.Value;
        }

        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        [HttpGet("openai")]
        public async Task<IActionResult> TestOpenAIConnection()
        {
            try
            {
                _logger.LogInformation("Testing OpenAI connection...");

                // Test with a simple question
                var questions = await _openAIService.GenerateQuizQuestionsAsync("simple test", 1);

                if (questions != null && questions.Count > 0)
                {
                    _logger.LogInformation("OpenAI connection test successful - received {QuestionCount} questions", questions.Count);
                    return Ok(new
                    {
                        status = "success",
                        message = $"OpenAI connection successful. Generated {questions.Count} question(s).",
                        timestamp = DateTime.UtcNow,
                        questionCount = questions.Count
                    });
                }
                else
                {
                    _logger.LogWarning("OpenAI connection test returned no questions or empty result");
                    return Ok(new
                    {
                        status = "warning",
                        message = "OpenAI connection succeeded but returned no questions. This might indicate an issue with the prompt or configuration.",
                        timestamp = DateTime.UtcNow,
                        questionCount = 0
                    });
                }
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "OpenAI configuration missing");
                return BadRequest(new
                {
                    status = "error",
                    message = "OpenAI configuration is missing. Please check appsettings.json.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI network error");
                return BadRequest(new
                {
                    status = "error",
                    message = "Network error connecting to OpenAI. Check your internet connection and API endpoint.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI connection test failed");
                return BadRequest(new
                {
                    status = "error",
                    message = "OpenAI connection test failed. Check your API key and configuration.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }

        [HttpGet("api")]
        public IActionResult TestAPIHealth()
        {
            try
            {
                // Test internal API health
                var result = new
                {
                    status = "success",
                    message = "API is responding correctly",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    version = "1.0.0"
                };

                _logger.LogInformation("API health check successful");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API health check failed");
                return BadRequest(new
                {
                    status = "error",
                    message = "API health check failed",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }

        [HttpGet("internet")]
        public async Task<IActionResult> TestInternetConnection()
        {
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync("www.google.com", 5000); // 5 second timeout

                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    _logger.LogInformation("Internet connection test successful");
                    return Ok(new
                    {
                        status = "success",
                        message = "Internet connection is active.",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("Internet connection test failed: {Status}", reply.Status);
                    return BadRequest(new
                    {
                        status = "error",
                        message = $"Internet connection failed: {reply.Status}",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during internet connection test");
                return BadRequest(new
                {
                    status = "error",
                    message = $"Error checking internet connection: {ex.Message}",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }

        [HttpGet("tablestorage")]
        public async Task<IActionResult> TestTableStorageConnection()
        {
            try
            {
                _logger.LogInformation("Testing Table Storage connection...");

                // Determine if we're using Azurite or Azure Table Storage
                bool isAzurite = _tableStorageSettings.ConnectionString.Contains("UseDevelopmentStorage=true",
                    StringComparison.OrdinalIgnoreCase);
                string storageType = isAzurite ? "Azurite (Local)" : "Azure Table Storage";

                // Create a test table name
                string testTableName = $"DiagnosticTest{DateTime.UtcNow:yyyyMMddHHmmss}";

                try
                {
                    // Test 1: Create a test table
                    var tableClient = _tableServiceClient.GetTableClient(testTableName);
                    await tableClient.CreateIfNotExistsAsync();
                    _logger.LogInformation("Successfully created test table {TableName}", testTableName);

                    // Test 2: Add a test entity
                    var testEntity = new TableEntity("TestPartition", "TestRow")
                    {
                        { "TestProperty", "TestValue" },
                        { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
                    };
                    await tableClient.AddEntityAsync(testEntity);
                    _logger.LogInformation("Successfully added test entity to table {TableName}", testTableName);

                    // Test 3: Retrieve the test entity
                    var retrievedEntity = await tableClient.GetEntityAsync<TableEntity>("TestPartition", "TestRow");
                    if (retrievedEntity?.Value != null &&
                        retrievedEntity.Value.GetString("TestProperty") == "TestValue")
                    {
                        _logger.LogInformation("Successfully retrieved test entity from table {TableName}", testTableName);
                    }
                    else
                    {
                        throw new InvalidOperationException("Retrieved entity does not match expected values");
                    }

                    // Test 4: Query entities
                    var queryResults = new List<TableEntity>();
                    await foreach (var entity in tableClient.QueryAsync<TableEntity>())
                    {
                        queryResults.Add(entity);
                    }

                    if (queryResults.Count == 0)
                    {
                        throw new InvalidOperationException("Query returned no results");
                    }

                    // Test 5: Delete the test entity
                    await tableClient.DeleteEntityAsync("TestPartition", "TestRow");
                    _logger.LogInformation("Successfully deleted test entity from table {TableName}", testTableName);

                    // Cleanup: Delete the test table
                    await tableClient.DeleteAsync();
                    _logger.LogInformation("Successfully deleted test table {TableName}", testTableName);

                    return Ok(new
                    {
                        status = "success",
                        message = $"{storageType} connection successful. Performed full CRUD operations test.",
                        timestamp = DateTime.UtcNow,
                        storageType = storageType,
                        connectionString = isAzurite ? "UseDevelopmentStorage=true" : "[Azure Storage Account]"
                    });
                }
                catch (Exception ex)
                {
                    // Cleanup on error: try to delete test table if it exists
                    try
                    {
                        var tableClient = _tableServiceClient.GetTableClient(testTableName);
                        await tableClient.DeleteAsync();
                        _logger.LogInformation("Cleaned up test table {TableName} after error", testTableName);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to cleanup test table {TableName}", testTableName);
                    }

                    _logger.LogError(ex, "Table Storage test failed");
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = $"Table Storage test failed: {ex.Message}",
                        timestamp = DateTime.UtcNow,
                        error = ex.Message
                    });
                }
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Table Storage configuration missing");
                return BadRequest(new
                {
                    status = "error",
                    message = "Table Storage configuration is missing. Please check appsettings.json.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "AuthenticationFailed")
            {
                _logger.LogError(ex, "Table Storage authentication failed");
                return BadRequest(new
                {
                    status = "error",
                    message = "Table Storage authentication failed. Check connection string and credentials.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "ResourceNotFound")
            {
                _logger.LogError(ex, "Table Storage resource not found");
                return BadRequest(new
                {
                    status = "error",
                    message = "Table Storage resource not found. Check if storage account exists.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogError(ex, "Network error connecting to Table Storage");
                bool isAzurite = _tableStorageSettings.ConnectionString.Contains("UseDevelopmentStorage=true",
                    StringComparison.OrdinalIgnoreCase);
                string suggestion = isAzurite
                    ? "Make sure Azurite is running (try 'azurite --silent --location ./AzuriteData --debug ./AzuriteData/debug.log')"
                    : "Check your internet connection and Azure Storage account availability.";

                return BadRequest(new
                {
                    status = "error",
                    message = $"Network error connecting to Table Storage. {suggestion}",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Table Storage connection test failed");
                return BadRequest(new
                {
                    status = "error",
                    message = "Table Storage connection test failed. Check your configuration and service availability.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }
    }
}
