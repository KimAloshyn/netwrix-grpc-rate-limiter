using YourRateLimiterService.Services;

namespace RateLimiter.Tests;

public class RateLimiterConcurrencyTests
{
    [Fact]
    public void TryAcquire_WhenRequestsAreConcurrent_NeverExceedsLimit()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 100, 60);

        int allowedCount = 0;

        // Act
        Parallel.For(0, 500, _ =>
        {
            if (limiter.TryAcquire("user1", "api/users"))
            {
                Interlocked.Increment(ref allowedCount);
            }
        });

        // Assert
        Assert.Equal(100, allowedCount);
    }

    [Fact]
    public void TryAcquire_WhenDifferentClientsAreConcurrent_TracksLimitsIndependently()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 50, 60);

        int user1Allowed = 0;
        int user2Allowed = 0;

        // Act
        Parallel.For(0, 1000, i =>
        {
            if (i % 2 == 0)
            {
                if (limiter.TryAcquire("user1", "api/users"))
                {
                    Interlocked.Increment(ref user1Allowed);
                }
            }
            else
            {
                if (limiter.TryAcquire("user2", "api/users"))
                {
                    Interlocked.Increment(ref user2Allowed);
                }
            }
        });

        // Assert
        Assert.Equal(50, user1Allowed);
        Assert.Equal(50, user2Allowed);
    }

    [Fact]
    public void TryAcquire_WhenDifferentResourcesAreConcurrent_TracksLimitsIndependently()
    {
        // Arrange
        var limiter = new RateLimiterEngine();

        limiter.ConfigureResource("api/users", 100, 60);
        limiter.ConfigureResource("api/orders", 10, 60);

        int usersAllowed = 0;
        int ordersAllowed = 0;

        // Act
        Parallel.For(0, 1000, i =>
        {
            if (i % 2 == 0)
            {
                if (limiter.TryAcquire("user1", "api/users"))
                {
                    Interlocked.Increment(ref usersAllowed);
                }
            }
            else
            {
                if (limiter.TryAcquire("user1", "api/orders"))
                {
                    Interlocked.Increment(ref ordersAllowed);
                }
            }
        });

        // Assert
        Assert.Equal(100, usersAllowed);
        Assert.Equal(10, ordersAllowed);
    }
}
