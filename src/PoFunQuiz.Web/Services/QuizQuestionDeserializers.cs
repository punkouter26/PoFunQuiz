using PoFunQuiz.Core.Models;
using System.Text.Json;

namespace PoFunQuiz.Web.Services;

/// <summary>
/// Interface for JSON deserialization strategies
/// </summary>
public interface IQuizQuestionDeserializer
{
    bool TryDeserialize(string json, out List<QuizQuestion> questions);
}

/// <summary>
/// Deserializes JSON with schema wrapper format: { "questions": [...] }
/// </summary>
public class SchemaWrapperDeserializer : IQuizQuestionDeserializer
{
    private readonly ILogger<SchemaWrapperDeserializer> _logger;

    public SchemaWrapperDeserializer(ILogger<SchemaWrapperDeserializer> logger)
    {
        _logger = logger;
    }

    public bool TryDeserialize(string json, out List<QuizQuestion> questions)
    {
        questions = new List<QuizQuestion>();

        try
        {
            _logger.LogInformation("Attempting schema wrapper deserialization...");
            var schemaResponse = JsonSerializer.Deserialize<JsonDocument>(json);

            if (schemaResponse?.RootElement.TryGetProperty("questions", out var questionsProperty) == true)
            {
                var result = JsonSerializer.Deserialize<List<QuizQuestion>>(
                    questionsProperty.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                questions = result ?? new List<QuizQuestion>();

                if (questions.Count != 0)
                {
                    _logger.LogInformation("✅ Schema wrapper deserialization successful: {Count} questions", questions.Count);
                    return true;
                }
            }

            _logger.LogInformation("Schema wrapper format not detected");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug("Schema wrapper deserialization failed: {Error}", ex.Message);
            return false;
        }
    }
}

/// <summary>
/// Deserializes JSON as a direct array: [{ ... }, { ... }]
/// </summary>
public class DirectArrayDeserializer : IQuizQuestionDeserializer
{
    private readonly ILogger<DirectArrayDeserializer> _logger;

    public DirectArrayDeserializer(ILogger<DirectArrayDeserializer> logger)
    {
        _logger = logger;
    }

    public bool TryDeserialize(string json, out List<QuizQuestion> questions)
    {
        questions = new List<QuizQuestion>();

        try
        {
            _logger.LogInformation("Attempting direct array deserialization...");
            var result = JsonSerializer.Deserialize<List<QuizQuestion>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            questions = result ?? new List<QuizQuestion>();

            if (questions.Count != 0)
            {
                _logger.LogInformation("✅ Direct array deserialization successful: {Count} questions", questions.Count);
                return true;
            }

            _logger.LogInformation("Direct array format resulted in empty list");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug("Direct array deserialization failed: {Error}", ex.Message);
            return false;
        }
    }
}

/// <summary>
/// Deserializes JSON as a single object and wraps it in a list: { ... }
/// </summary>
public class SingleObjectDeserializer : IQuizQuestionDeserializer
{
    private readonly ILogger<SingleObjectDeserializer> _logger;

    public SingleObjectDeserializer(ILogger<SingleObjectDeserializer> logger)
    {
        _logger = logger;
    }

    public bool TryDeserialize(string json, out List<QuizQuestion> questions)
    {
        questions = new List<QuizQuestion>();

        try
        {
            _logger.LogInformation("Attempting single object deserialization...");
            var singleQuestion = JsonSerializer.Deserialize<QuizQuestion>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (singleQuestion != null)
            {
                questions = new List<QuizQuestion> { singleQuestion };
                _logger.LogInformation("✅ Single object deserialization successful");
                return true;
            }

            _logger.LogInformation("Single object deserialization resulted in null");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug("Single object deserialization failed: {Error}", ex.Message);
            return false;
        }
    }
}
