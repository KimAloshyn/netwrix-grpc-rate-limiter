using YourRateLimiterService.Services;

namespace RateLimiter.Tests;

public class RateLimiterRequestTests
{
    [Fact]
    public void TryAcquire_WhenWithinLimit_AllowsRequests()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 3, 60);

        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            Assert.True(limiter.TryAcquire("user1", "api/users"));
        }
    }

    [Fact]
    public void TryAcquire_WhenLimitExceeded_DeniesRequests()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 2, 60);

        // Act
        bool first = limiter.TryAcquire("user1", "api/users");
        bool second = limiter.TryAcquire("user1", "api/users");
        bool third = limiter.TryAcquire("user1", "api/users");
        bool fourth = limiter.TryAcquire("user1", "api/users");

        // Assert
        Assert.True(first);
        Assert.True(second);
        Assert.False(third);
        Assert.False(fourth);
    }

    [Fact]
    public void TryAcquire_WhenClientsDiffer_TracksLimitsIndependently()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 1, 60);

        // Act & Assert
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user1", "api/users"));

        Assert.True(limiter.TryAcquire("user2", "api/users"));
        Assert.False(limiter.TryAcquire("user2", "api/users"));
    }

    [Fact]
    public void TryAcquire_WhenResourcesDiffer_TracksLimitsIndependently()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 1, 60);
        limiter.ConfigureResource("api/orders", 2, 60);

        // Act & Assert
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user1", "api/users"));

        Assert.True(limiter.TryAcquire("user1", "api/orders"));
        Assert.True(limiter.TryAcquire("user1", "api/orders"));
        Assert.False(limiter.TryAcquire("user1", "api/orders"));
    }

    [Fact]
    public void TryAcquire_WhenResourceIsNotConfigured_DeniesRequest()
    {
        // Arrange
        var limiter = new RateLimiterEngine();

        // Act
        bool allowed = limiter.TryAcquire("user1", "api/undefined");

        // Assert
        Assert.False(allowed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void TryAcquire_WhenClientIdIsInvalid_DeniesRequest(
        string clientId)
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 10, 60);

        // Act
        bool allowed = limiter.TryAcquire(clientId, "api/users");

        // Assert
        Assert.False(allowed);
    }
}
