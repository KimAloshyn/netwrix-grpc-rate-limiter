namespace YourRateLimiterService.Models;

public sealed class WindowState
{
    public object SyncRoot { get; } = new();
    public DateTimeOffset WindowStart { get; set; } = DateTimeOffset.MinValue;
    public int Count { get; set; } = 0;
    public long ConfigVersion { get; set; } = -1; // -1 means window hasn't been associated with a configuration yet
}
