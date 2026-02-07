using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Options;
using PoFunQuiz.Web.Models;
using PoFunQuiz.Web.Configuration;
using OpenAI.Chat;

namespace PoFunQuiz.Web.Features.Quiz;

/// <summary>
/// Interface for quiz question generation via OpenAI.
/// </summary>
public interface IOpenAIService
{
    Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions);
}

/// <summary>
/// Service for generating quiz questions using Azure OpenAI.
/// </summary>
public class OpenAIService : IOpenAIService
{
    private ChatClient? _chatClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly OpenAISettings _settings;

    public OpenAIService(
        IOptions<OpenAISettings> openAISettings,
        ILogger<OpenAIService> logger)
    {
        _logger = logger;
        _settings = openAISettings.Value;
    }

    /// <summary>
    /// Returns the lazily-initialized ChatClient, creating it on first access.
    /// </summary>
    private ChatClient GetChatClient()
    {
        return _chatClient ??= InitializeChatClient();
    }

    private ChatClient InitializeChatClient()
    {
        var endpoint = _settings.Endpoint;
        var apiKey = _settings.ApiKey;
        var deploymentName = _settings.DeploymentName;

        _logger.LogInformation("OpenAI Configuration - Endpoint: {Endpoint}, DeploymentName: {DeploymentName}", endpoint, deploymentName);

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
        {
            _logger.LogError("Azure OpenAI configuration is missing. Endpoint: {Endpoint}, ApiKey: {ApiKeyPresent}, DeploymentName: {DeploymentName}",
                endpoint, !string.IsNullOrEmpty(apiKey), deploymentName);
            throw new ArgumentNullException("Azure OpenAI configuration is missing. Please check appsettings.json.");
        }

        var baseUri = new Uri(endpoint);
        _logger.LogInformation("Base URI: {BaseUri}", baseUri.ToString());

        var azureClient = new AzureOpenAIClient(baseUri, new AzureKeyCredential(apiKey));
        return azureClient.GetChatClient(deploymentName);
    }

    public async Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions)
    {
        _logger.LogInformation("Generating {Count} quiz questions for topic '{Topic}'", numberOfQuestions, topic);

        try
        {
            var jsonResponse = await FetchOpenAIResponse(topic, numberOfQuestions);

            if (string.IsNullOrEmpty(jsonResponse))
            {
                _logger.LogWarning("OpenAI returned an empty response for topic '{Topic}'", topic);
                return new List<QuizQuestion>();
            }

            return DeserializeQuestions(jsonResponse, topic);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "OpenAI request timed out or was cancelled for topic '{Topic}'", topic);
            return new List<QuizQuestion>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling OpenAI for topic '{Topic}'. Status: {StatusCode}", topic, ex.StatusCode);
            return new List<QuizQuestion>();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure OpenAI API error for topic '{Topic}'. Status: {Status}, Code: {Code}", topic, ex.Status, ex.ErrorCode);
            return new List<QuizQuestion>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating quiz questions for topic '{Topic}'", topic);
            return new List<QuizQuestion>();
        }
    }

    private async Task<string> FetchOpenAIResponse(string topic, int numberOfQuestions)
    {
        var messages = BuildChatMessages(topic, numberOfQuestions);
        var options = BuildChatOptions();

        _logger.LogInformation("Calling ChatClient.CompleteChatAsync...");
        ChatCompletion response = await GetChatClient().CompleteChatAsync(messages, options);
        string jsonResponse = response.Content[0].Text ?? string.Empty;

        _logger.LogInformation("Received response: {Length} characters", jsonResponse.Length);
        _logger.LogDebug("Raw JSON response: {JsonResponse}", jsonResponse);

        return jsonResponse;
    }

    private static List<ChatMessage> BuildChatMessages(string topic, int numberOfQuestions)
    {
        return new List<ChatMessage>
        {
            new SystemChatMessage($@"You are a helpful assistant designed to output JSON.
                Generate {numberOfQuestions} multiple-choice quiz questions about {topic}.
                Each question should have a 'Question' (string), 'Options' (an array of exactly 4 strings), and a 'CorrectOptionIndex' (integer, 0-based index of the correct option in the 'Options' array).
                IMPORTANT: Return a JSON object with a 'questions' property containing an array of question objects.
                Ensure the JSON follows this exact structure:
                {{
                    ""questions"": [
                        {{
                            ""Question"": ""What is the capital of France?"",
                            ""Options"": [""Berlin"", ""Madrid"", ""Paris"", ""Rome""],
                            ""CorrectOptionIndex"": 2
                        }},
                        {{
                            ""Question"": ""Which planet is known as the Red Planet?"",
                            ""Options"": [""Earth"", ""Mars"", ""Jupiter"", ""Venus""],
                            ""CorrectOptionIndex"": 1
                        }}
                    ]
                }}"),
            new UserChatMessage($"Generate exactly {numberOfQuestions} questions about {topic}. Return as a JSON array.")
        };
    }

    private static ChatCompletionOptions BuildChatOptions()
    {
        return new ChatCompletionOptions
        {
            Temperature = 0.7f,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };
    }

    private List<QuizQuestion> DeserializeQuestions(string json, string topic)
    {
        var questions = QuizQuestionDeserializer.Deserialize(json, _logger);
        if (questions.Count > 0)
        {
            _logger.LogInformation("Deserialized {Count} questions for topic '{Topic}'", questions.Count, topic);
        }
        return questions;
    }
}
