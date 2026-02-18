using PoFunQuiz.Web.Models;
using System.Text.Json;

namespace PoFunQuiz.Web.Features.Quiz;

/// <summary>
/// Utility for deserializing quiz questions from OpenAI JSON responses.
/// Tries wrapper format first, then direct array, then single object.
/// Validates each question has exactly 4 options with a valid CorrectOptionIndex.
/// </summary>
public static class QuizQuestionDeserializer
{
    private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    public static List<QuizQuestion> Deserialize(string json, ILogger logger)
    {
        // Try { "questions": [...] } wrapper
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("questions", out var questionsProperty))
            {
                var result = JsonSerializer.Deserialize<List<QuizQuestion>>(questionsProperty.GetRawText(), CaseInsensitive);
                if (result is { Count: > 0 })
                {
                    logger.LogInformation("Deserialized {Count} questions (wrapper format)", result.Count);
                    return ValidateQuestions(result, logger);
                }
            }
        }
        catch (JsonException) { }

        // Try direct array [...]
        try
        {
            var result = JsonSerializer.Deserialize<List<QuizQuestion>>(json, CaseInsensitive);
            if (result is { Count: > 0 })
            {
                logger.LogInformation("Deserialized {Count} questions (array format)", result.Count);
                return ValidateQuestions(result, logger);
            }
        }
        catch (JsonException) { }

        // Try single object { ... }
        try
        {
            var single = JsonSerializer.Deserialize<QuizQuestion>(json, CaseInsensitive);
            if (single != null)
            {
                logger.LogInformation("Deserialized 1 question (single object format)");
                return ValidateQuestions([single], logger);
            }
        }
        catch (JsonException) { }

        logger.LogError("All deserialization attempts failed. Raw JSON: {Json}", json);
        return [];
    }

    /// <summary>
    /// Filters out questions with invalid Options count or out-of-range CorrectOptionIndex.
    /// </summary>
    private static List<QuizQuestion> ValidateQuestions(List<QuizQuestion> questions, ILogger logger)
    {
        var valid = new List<QuizQuestion>(questions.Count);
        foreach (var q in questions)
        {
            if (q.Options.Count == 0)
            {
                logger.LogWarning("Skipping question with no options: {Question}", q.Question);
                continue;
            }

            if (q.CorrectOptionIndex < 0 || q.CorrectOptionIndex >= q.Options.Count)
            {
                logger.LogWarning(
                    "Skipping question with invalid CorrectOptionIndex {Index} (Options count: {Count}): {Question}",
                    q.CorrectOptionIndex, q.Options.Count, q.Question);
                continue;
            }

            if (string.IsNullOrWhiteSpace(q.Question))
            {
                logger.LogWarning("Skipping question with empty text");
                continue;
            }

            valid.Add(q);
        }

        if (valid.Count < questions.Count)
        {
            logger.LogWarning("Filtered {Removed} invalid questions, {Valid} remaining",
                questions.Count - valid.Count, valid.Count);
        }

        return valid;
    }
}
