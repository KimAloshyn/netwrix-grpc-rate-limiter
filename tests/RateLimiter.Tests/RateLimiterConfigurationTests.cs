using YourRateLimiterService.Services;

namespace RateLimiter.Tests;

public class RateLimiterConfigurationTests
{
    [Fact]
    public void ConfigureResource_WhenValid_ReturnsTrue()
    {
        // Arrange
        var limiter = new RateLimiterEngine();

        // Act
        bool success = limiter.ConfigureResource("api/users", 100, 60);

        // Assert
        Assert.True(success);
    }

    [Theory]
    [InlineData("", 10, 60)]
    [InlineData(" ", 10, 60)]
    [InlineData("api/users", 0, 60)]
    [InlineData("api/users", -1, 60)]
    [InlineData("api/users", 10, 0)]
    [InlineData("api/users", 10, -1)]
    public void ConfigureResource_WhenInvalid_ReturnsFalse(
        string resource,
        int maxRequests,
        int windowSeconds)
    {
        // Arrange
        var limiter = new RateLimiterEngine();

        // Act
        bool success = limiter.ConfigureResource(resource, maxRequests, windowSeconds);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void ConfigureResource_WhenInvalidUpdate_PreservesExistingLimit()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 1, 60);

        // Act
        bool updated = limiter.ConfigureResource("api/users", 0, 60);

        // Assert
        Assert.False(updated);
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user1", "api/users"));
    }

    [Fact]
    public void ConfigureResource_WhenConfigurationChanges_ResetsCounter()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 3, 60);

        for (int i = 0; i < 3; i++)
        {
            Assert.True(limiter.TryAcquire("user1", "api/users"));
        }

        Assert.False(limiter.TryAcquire("user1", "api/users"));

        // Act
        bool updated = limiter.ConfigureResource("api/users", 1, 60);

        // Assert
        Assert.True(updated);
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user1", "api/users"));
    }

    [Fact]
    public void ConfigureResource_WhenConfigurationIsIdentical_PreservesCounter()
    {
        // Arrange
        var limiter = new RateLimiterEngine();
        limiter.ConfigureResource("api/users", 1, 60);

        Assert.True(limiter.TryAcquire("user1", "api/users"));

        // Act
        bool updated = limiter.ConfigureResource("api/users", 1, 60);

        // Assert
        Assert.True(updated);
        Assert.False(limiter.TryAcquire("user1", "api/users"));
    }

    [Fact]
    public void ConfigureResource_WhenOneResourceChanges_DoesNotResetOtherResource()
    {
        // Arrange
        var limiter = new RateLimiterEngine();

        limiter.ConfigureResource("api/users", 1, 60);
        limiter.ConfigureResource("api/orders", 1, 60);

        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.True(limiter.TryAcquire("user1", "api/orders"));

        // Act
        limiter.ConfigureResource("api/users", 2, 60);

        // Assert
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.True(limiter.TryAcquire("user1", "api/users"));
        Assert.False(limiter.TryAcquire("user1", "api/users"));

        Assert.False(limiter.TryAcquire("user1", "api/orders"));
    }
}
