namespace AxpoGroupChallenge.Reports.Domain.Entities
{
    using AxpoGroupChallenge.Reports.Domain.ValueObjects;

    /// <summary>
    /// Aggregate Root: Represents a trade with its aggregated power positions.
    /// </summary>
    public sealed class Trade
    {
        /// <summary>
        /// Unique trade identifier.
        /// </summary>
        public string TradeId { get; private set; }

        /// <summary>
        /// Collection of power positions aggregated from trade periods.
        /// </summary>
        public IReadOnlyList<PowerPosition> PowerPositions { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Trade(string tradeId, IReadOnlyList<PowerPosition> powerPositions)
        {
            if (string.IsNullOrWhiteSpace(tradeId))
                throw new ArgumentException(nameof(tradeId));

            if (powerPositions == null)
                throw new ArgumentNullException(nameof(powerPositions));

            TradeId = tradeId;
            PowerPositions = powerPositions;
        }
    }
}
