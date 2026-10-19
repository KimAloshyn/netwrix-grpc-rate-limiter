using System.Collections.Concurrent;
using YourRateLimiterService.Models;

namespace YourRateLimiterService.Services;

public sealed class RateLimiterEngine
{
    private readonly ConcurrentDictionary<string, ResourceConfig> _configs = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<(string ClientId, string Resource), WindowState> _windows = new();

    private readonly TimeProvider _timeProvider;

    public RateLimiterEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool ConfigureResource(string resource, int maxRequests, int windowSeconds)
    {
        if (string.IsNullOrWhiteSpace(resource) || maxRequests <= 0 || windowSeconds <= 0)
        {
            return false;
        }

        _configs.AddOrUpdate(resource, _ => new ResourceConfig(maxRequests, windowSeconds, Version: 1),
            (_, current) =>
            {
                if (current.MaxRequests == maxRequests && current.WindowSeconds == windowSeconds)
                {
                    return current;
                }

                return new ResourceConfig(maxRequests, windowSeconds, current.Version + 1);
            }
         );  

        return true;
    }

    public bool TryAcquire(string clientId, string resource)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(resource))
        {
            return false;
        }

        if (!_configs.ContainsKey(resource))
        {
            return false;
        }

        var key = (ClientId: clientId, Resource: resource);

        var state = _windows.GetOrAdd(key, _ => new WindowState());

        lock (state.SyncRoot)
        {
            if (!_configs.TryGetValue(resource, out var config))
            {
                return false;
            }

            var now = _timeProvider.GetUtcNow();
            var windowDuration = TimeSpan.FromSeconds(config.WindowSeconds);

            bool configChanged = state.ConfigVersion != config.Version;

            bool windowExpired = (now - state.WindowStart) >= windowDuration;

            if (configChanged || windowExpired)
            {
                state.WindowStart = now;
                state.Count = 0;
                state.ConfigVersion = config.Version;
            }

            if(state.Count >= config.MaxRequests)
            {
                return false;
            }

            state.Count++;

            return true;
        }
    }
}
