using Axpo;
using AxpoGroupChallenge.Reports.Application.Interfaces;

namespace AxpoGroupChallenge.Reports.Application.Services
{
    public sealed class PowerTradeAgregatorService : IPowerTradeAgregatorService
    {
        public IReadOnlyDictionary<int, decimal> AggregatePeriods(IEnumerable<PowerTrade> trades)
            => trades
                .Where(t => t?.Periods != null && t.Periods.Length > 0)
                .SelectMany(t => t.Periods)
                .GroupBy(p => p.Period)
                .ToDictionary(g => g.Key, g => g.Sum(p => (decimal)p.Volume));
    }
}
