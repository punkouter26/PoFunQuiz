using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PoFunQuiz.Core.Interfaces;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Web.Features.Leaderboard;

public static class SubmitScore
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/leaderboard", async (LeaderboardEntry entry, ILeaderboardRepository repository) =>
        {
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
