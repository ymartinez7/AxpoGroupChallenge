using Axpo;
using AxpoGroupChallenge.Reports.Application.Services;

namespace AxpoGroupChallenge.Reports.Application.UnitTests.Services;

public sealed class PowerTradeAgregatorServiceTests
{
    private readonly PowerTradeAgregatorService _powerTradeAgregatorService;

    public PowerTradeAgregatorServiceTests()
    {
        _powerTradeAgregatorService = new();
    }

    [Fact]
    public void AggregatePeriods_EmptyList_ReturnsEmptyDictionary()
    {
        var result = _powerTradeAgregatorService.AggregatePeriods([]);

        Assert.Empty(result);
    }

    [Fact]
    public void AggregatePeriods_NullTrade_IsIgnoredWithoutException()
    {
        var exception = Record.Exception(() => _powerTradeAgregatorService.AggregatePeriods([null!]));

        Assert.Null(exception);
    }

    [Fact]
    public void AggregatePeriods_SingleTrade_ContainsEntryForEachPeriod()
    {
        var trade = PowerTrade.Create(DateTime.Today, 24);

        var result = _powerTradeAgregatorService.AggregatePeriods([trade]);

        Assert.Equal(24, result.Count);
        Assert.True(result.ContainsKey(1));
        Assert.True(result.ContainsKey(24));
    }

    [Fact]
    public void AggregatePeriods_TwoTrades_ContainsEntriesForAllPeriods()
    {
        var trade1 = PowerTrade.Create(DateTime.Today, 24);
        var trade2 = PowerTrade.Create(DateTime.Today, 24);

        var result = _powerTradeAgregatorService.AggregatePeriods([trade1, trade2]);

        Assert.Equal(24, result.Count);
        for (int period = 1; period <= 24; period++)
            Assert.True(result.ContainsKey(period));
    }

    [Fact]
    public void AggregatePeriods_TwoTradesWithDefaultVolumes_SumsToZeroPerPeriod()
    {
        var trade1 = PowerTrade.Create(DateTime.Today, 24);
        var trade2 = PowerTrade.Create(DateTime.Today, 24);

        var result = _powerTradeAgregatorService.AggregatePeriods([trade1, trade2]);

        // Default volume in PowerTrade.Create is 0 — sum of two zeros is 0
        Assert.Equal(0m, result[1]);
    }

    [Fact]
    public void AggregatePeriods_ThreeTrades_AllPeriodsPresent()
    {
        var trades = Enumerable.Range(0, 3)
            .Select(_ => PowerTrade.Create(DateTime.Today, 24))
            .ToList();

        var result = _powerTradeAgregatorService.AggregatePeriods(trades);

        Assert.Equal(24, result.Count);
    }
}
