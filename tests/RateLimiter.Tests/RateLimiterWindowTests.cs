using Microsoft.Extensions.Time.Testing;
using YourRateLimiterService.Services;

namespace RateLimiter.Tests;

public class RateLimiterWindowTests
{
    [Fact]
    public void TryAcquire_WhenWindowHasNotExpired_PreservesCounter()
    {
        // Arrange
        var clock = new FakeTimeProvider();
        var limiter = new RateLimiterEngine(clock);

        limiter.ConfigureResource("api/users", 1, 60);

        Assert.True(limiter.TryAcquire("user1", "api/users"));

        // Act
        clock.Advance(TimeSpan.FromSeconds(59));

        // Assert
        Assert.False(limiter.TryAcquire("user1", "api/users"));
    }

    [Fact]
    public void TryAcquire_WhenWindowExpires_ResetsCounter()
    {
        // Arrange
        var clock = new FakeTimeProvider();
        var limiter = new RateLimiterEngine(clock);

        limiter.ConfigureResource("api/users", 2, 60);

        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user1", "api/users"));

        // Act
        clock.Advance(TimeSpan.FromSeconds(60));

        // Assert
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user1", "api/users"));
    }

    [Fact]
    public void TryAcquire_WhenClientsStartAtDifferentTimes_UsesIndependentWindows()
    {
        // Arrange
        var clock = new FakeTimeProvider();
        var limiter = new RateLimiterEngine(clock);

        limiter.ConfigureResource("api/users", 1, 60);

        // User1 starts at time 0.
        Assert.True(limiter.TryAcquire("user1", "api/users"));

        // User2 starts at time 30.
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(limiter.TryAcquire("user2", "api/users"));

        // Act: Advance to time 60.
        clock.Advance(TimeSpan.FromSeconds(30));

        // Assert: User1's window expired, but user2's did not.
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user2", "api/users"));
    }
}
