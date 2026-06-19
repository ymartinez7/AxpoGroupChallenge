using Axpo;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AxpoGroupChallenge.Reports.Infrastructure.UnitTests.Services;

public sealed class ResilientPowerServiceDecoratorTests
{
    private static readonly DateTime Date = new(2024, 1, 15);
    private static readonly IEnumerable<PowerTrade> EmptyTrades = [];

    [Fact]
    public void GetTrades_SuccessOnFirstAttempt_ReturnsResultWithoutRetry()
    {
        var inner = new Mock<IPowerService>();
        inner.Setup(s => s.GetTrades(Date)).Returns(EmptyTrades);
        var sut = Build(inner, maxRetryAttempts: 3);

        var result = sut.GetTrades(Date);

        Assert.Same(EmptyTrades, result);
        inner.Verify(s => s.GetTrades(Date), Times.Once);
    }

    [Fact]
    public void GetTrades_TransientFailureThenSuccess_ReturnsResultAfterRetry()
    {
        var inner = new Mock<IPowerService>();
        inner.SetupSequence(s => s.GetTrades(Date))
            .Throws<Exception>()
            .Returns(EmptyTrades);
        var sut = Build(inner, maxRetryAttempts: 3);

        var result = sut.GetTrades(Date);

        Assert.Same(EmptyTrades, result);
        inner.Verify(s => s.GetTrades(Date), Times.Exactly(2));
    }

    [Fact]
    public void GetTrades_AllAttemptsExhausted_ThrowsLastException()
    {
        var inner = new Mock<IPowerService>();
        inner.Setup(s => s.GetTrades(Date)).Throws<InvalidOperationException>();
        var sut = Build(inner, maxRetryAttempts: 2);

        Assert.Throws<InvalidOperationException>(() => sut.GetTrades(Date));
        inner.Verify(s => s.GetTrades(Date), Times.Exactly(3)); // 1 initial + 2 retries
    }

    [Fact]
    public void GetTrades_ZeroMaxRetryAttempts_ExceptionPropagatesAfterOneAttempt()
    {
        var inner = new Mock<IPowerService>();
        inner.Setup(s => s.GetTrades(Date)).Throws<InvalidOperationException>();
        var sut = Build(inner, maxRetryAttempts: 0);

        Assert.Throws<InvalidOperationException>(() => sut.GetTrades(Date));
        inner.Verify(s => s.GetTrades(Date), Times.Once);
    }

    [Fact]
    public async Task GetTradesAsync_DelegatesToInnerWithoutRetry()
    {
        var inner = new Mock<IPowerService>();
        inner.Setup(s => s.GetTradesAsync(Date)).ReturnsAsync(EmptyTrades);
        var sut = Build(inner, maxRetryAttempts: 3);

        var result = await sut.GetTradesAsync(Date);

        Assert.Same(EmptyTrades, result);
        inner.Verify(s => s.GetTradesAsync(Date), Times.Once);
    }

    private static ResilientPowerServiceDecorator Build(Mock<IPowerService> inner, int maxRetryAttempts)
    {
        var options = Options.Create(new RetryOptions
        {
            MaxRetryAttempts = maxRetryAttempts,
            BaseDelay = TimeSpan.Zero
        });
        return new ResilientPowerServiceDecorator(inner.Object, options, NullLogger<ResilientPowerServiceDecorator>.Instance);
    }
}
