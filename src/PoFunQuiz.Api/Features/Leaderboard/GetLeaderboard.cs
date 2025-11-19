using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PoFunQuiz.Core.Interfaces;
using PoFunQuiz.Core.Models;
using System.Collections.Generic;

namespace PoFunQuiz.Api.Features.Leaderboard
{
    public static class GetLeaderboard
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/leaderboard", async (string category, ILeaderboardRepository repository) =>
            {
                var scores = await repository.GetTopScoresAsync(category ?? "General");
                return Results.Ok(scores);
            })
            .WithName("GetLeaderboard")
            .WithOpenApi();
        }
    }
}
