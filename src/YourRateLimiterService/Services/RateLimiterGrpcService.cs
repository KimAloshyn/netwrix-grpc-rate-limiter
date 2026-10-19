using Grpc.Core;
using RateLimiter.Grpc;

namespace YourRateLimiterService.Services;

public class RateLimiterGrpcService : global::RateLimiter.Grpc.RateLimiter.RateLimiterBase
{
    private readonly RateLimiterEngine _rateLimiter;

    public RateLimiterGrpcService(RateLimiterEngine rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    public override Task<RateLimitResponse> CheckRateLimit(
        RateLimitRequest request,
        ServerCallContext context)
    {
        bool allowed = _rateLimiter.TryAcquire(request.ClientId, request.Resource);

        return Task.FromResult(new RateLimitResponse
        {
            Allowed = allowed
        });
    }

    public override Task<ConfigureResourceResponse> ConfigureResource(
        ConfigureResourceRequest request,
        ServerCallContext context)
    {
        bool success = _rateLimiter.ConfigureResource(
            request.Resource,
            request.MaxRequests,
            request.WindowSeconds);

        return Task.FromResult(new ConfigureResourceResponse
        {
            Success = success
        });
    }
}

