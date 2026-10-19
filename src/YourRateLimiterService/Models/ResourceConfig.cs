namespace YourRateLimiterService.Models;

public sealed record ResourceConfig(
    int MaxRequests,
    int WindowSeconds,
    long Version);