using Axpo;
using AxpoGroupChallenge.Reports.Application.Interfaces;

namespace AxpoGroupChallenge.Reports.Application.Services
{
    public sealed class PowerTradeAgregatorService : IPowerTradeAgregatorService
    {
        public IReadOnlyDictionary<int, decimal> AggregatePeriods(IEnumerable<PowerTrade> trades)
        {
            var periodVolumes = new Dictionary<int, decimal>();

            foreach (var trade in trades)
            {
                if (trade?.Periods == null || trade.Periods.Length == 0)
                    continue;

                foreach (var period in trade.Periods)
                {
                    if (!periodVolumes.ContainsKey(period.Period))
                        periodVolumes[period.Period] = 0;

                    periodVolumes[period.Period] += (decimal)period.Volume;
                }
            }

            return periodVolumes;
        }
    }
}
