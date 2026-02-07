using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PoFunQuiz.Web.Models;
using System.Text.RegularExpressions;

namespace PoFunQuiz.Web.Features.Leaderboard;

public static partial class SubmitScore
{
    private const int MaxPlayerNameLength = 20;
    private const int MaxScore = 10_000;

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/leaderboard", async (LeaderboardEntry entry, ILeaderboardRepository repository) =>
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(entry.PlayerName))
            {
                return Results.BadRequest("PlayerName is required.");
            }

            // Sanitize and clamp
            entry.PlayerName = HtmlTagRegex().Replace(entry.PlayerName.Trim(), string.Empty);
            if (entry.PlayerName.Length > MaxPlayerNameLength)
            {
                entry.PlayerName = entry.PlayerName[..MaxPlayerNameLength];
            }

            if (entry.Score < 0)
            {
                return Results.BadRequest("Score must be non-negative.");
            }

            entry.Score = Math.Min(entry.Score, MaxScore);

            // Validate category is known
            if (!string.IsNullOrEmpty(entry.Category) &&
                !QuestionCategories.All.Contains(entry.Category, StringComparer.OrdinalIgnoreCase))
            {
                entry.Category = "General";
            }

            entry.Timestamp = DateTimeOffset.UtcNow;
            entry.RowKey = Guid.NewGuid().ToString();
            // Ensure PartitionKey matches Category for efficient querying
            entry.PartitionKey = entry.Category ?? "General";

            await repository.AddScoreAsync(entry);
            return Results.Created($"/api/leaderboard/{entry.RowKey}", entry);
        })
        .WithName("SubmitScore")
        .WithOpenApi();
    }
}
