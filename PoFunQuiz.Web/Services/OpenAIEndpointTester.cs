using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;

namespace PoFunQuiz.Web.Services
{
    /// <summary>
    /// Simple test class to verify how the Azure OpenAI SDK processes endpoints
    /// </summary>
    public class OpenAIEndpointTester
    {
        private readonly ILogger<OpenAIEndpointTester> _logger;

        public OpenAIEndpointTester(ILogger<OpenAIEndpointTester> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tests how the Azure OpenAI SDK processes different endpoint formats
        /// </summary>
        public async Task<string> TestEndpointHandling(string endpoint, string apiKey, string deploymentName)
        {
            try
            {
                _logger.LogInformation("Testing Azure OpenAI endpoint: {Endpoint}", endpoint);
                
                var result = new System.Text.StringBuilder();
                result.AppendLine($"Testing endpoint: {endpoint}");
                
                // Create a client with the provided endpoint
                var client = new OpenAIClient(
                    new Uri(endpoint),
                    new AzureKeyCredential(apiKey));
                
                result.AppendLine("Client created successfully");
                
                // Get the endpoint URI from client's Pipeline (if possible through reflection)
                try
                {
                    var clientType = client.GetType();
                    var pipelineField = clientType.GetField("_pipeline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (pipelineField != null)
                    {
                        var pipeline = pipelineField.GetValue(client);
                        if (pipeline != null)
                        {
                            var pipelineType = pipeline.GetType();
                            var httpPipelineTransportField = pipelineType.GetField("_httpPipelineTransport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (httpPipelineTransportField != null)
                            {
                                var transport = httpPipelineTransportField.GetValue(pipeline);
                                result.AppendLine($"Transport type: {transport?.GetType().FullName}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine($"Error examining client internals: {ex.Message}");
                }
                
                // Try to make a simple chat completion request
                try
                {
                    _logger.LogInformation("Making test API call to deployment: {Deployment}", deploymentName);
                    
                    var messages = new List<ChatRequestMessage>
                    {
                        new ChatRequestSystemMessage("You are a helpful assistant."),
                        new ChatRequestUserMessage("Return only the text 'SUCCESS' as your response")
                    };

                    var chatCompletionsOptions = new ChatCompletionsOptions(deploymentName, messages)
                    {
                        MaxTokens = 10
                    };

                    var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
                    var responseText = response.Value.Choices[0].Message.Content;
                    
                    result.AppendLine($"API call successful");
                    result.AppendLine($"Response: {responseText}");
                }
                catch (Exception ex)
                {
                    result.AppendLine($"API call failed: {ex.GetType().Name}: {ex.Message}");
                    
                    if (ex is RequestFailedException rfe)
                    {
                        result.AppendLine($"Status: {rfe.Status}");
                        result.AppendLine($"ErrorCode: {rfe.ErrorCode}");
                        
                        // Check inner exception for more details
                        if (rfe.InnerException != null)
                        {
                            result.AppendLine($"Inner exception: {rfe.InnerException.GetType().Name}: {rfe.InnerException.Message}");
                            
                            // If it's a DNS error, this is particularly relevant
                            if (rfe.InnerException is System.Net.Http.HttpRequestException httpEx && 
                                httpEx.InnerException is System.Net.Sockets.SocketException socketEx)
                            {
                                result.AppendLine($"Socket error code: {socketEx.SocketErrorCode}");
                                result.AppendLine($"This is a DNS resolution error - host name could not be found");
                            }
                        }
                    }
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing endpoint handling");
                return $"Error testing endpoint: {ex.GetType().Name}: {ex.Message}";
            }
        }
    }
}