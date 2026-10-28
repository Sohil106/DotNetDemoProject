namespace InterviewLab.Services;

public sealed class DemoCounters
{
    // C# Q53; Async/Multithreading Q286: Interlocked protects a shared counter from racing increments.
    private long _requests;
    public long Increment() => Interlocked.Increment(ref _requests);
    public long Current => Interlocked.Read(ref _requests);
}

public sealed class HeartbeatService(ILogger<HeartbeatService> logger) : BackgroundService
{
    // .NET Core & Web API Q20, Q32, Q33: hosted background work uses the host cancellation token for shutdown.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Background heartbeat at {UtcNow}", DateTimeOffset.UtcNow);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected when the host is stopping; don't surface normal shutdown as an unhandled exception.
            logger.LogInformation("Background heartbeat is stopping.");
        }
    }
}
