using Axpo;

namespace AxpoGroupChallenge.Reports.Application.Interfaces
{
    public interface IPowerTradeAgregatorService
    {
        IReadOnlyDictionary<int, decimal> AggregatePeriods(IEnumerable<PowerTrade> trades);
    }
}
