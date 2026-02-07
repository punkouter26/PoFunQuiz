using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web.Features.Leaderboard;

public static class GetLeaderboard
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/leaderboard", async (string? category, ILeaderboardRepository repository) =>
        {
            var scores = await repository.GetTopScoresAsync(category ?? "General");
            return Results.Ok(scores);
        })
        .WithName("GetLeaderboard")
        .WithOpenApi();
    }
}
