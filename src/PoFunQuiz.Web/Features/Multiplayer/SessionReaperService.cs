namespace PoFunQuiz.Web.Features.Multiplayer;

/// <summary>
/// Background service that periodically purges finished or stale multiplayer sessions
/// to prevent unbounded memory growth in the <see cref="MultiplayerLobbyService"/> singleton.
/// </summary>
public class SessionReaperService : BackgroundService
{
    private readonly MultiplayerLobbyService _lobbyService;
    private readonly ILogger<SessionReaperService> _logger;

    /// <summary>Sessions completed/started more than this long ago are eligible for removal.</summary>
    private static readonly TimeSpan SessionMaxAge = TimeSpan.FromHours(2);

    /// <summary>How often the reaper sweep runs.</summary>
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(15);

    public SessionReaperService(MultiplayerLobbyService lobbyService, ILogger<SessionReaperService> logger)
    {
        _lobbyService = lobbyService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionReaperService started — sweeping every {Interval}", SweepInterval);

        using var timer = new PeriodicTimer(SweepInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _lobbyService.PurgeExpiredSessions(SessionMaxAge);
                _logger.LogDebug("SessionReaperService sweep completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SessionReaperService sweep failed");
            }
        }
    }
}
