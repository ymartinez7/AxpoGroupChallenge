namespace AxpoGroupChallenge.Reports.Domain.ValueObjects
{
    using System.Globalization;

    /// <summary>
    /// Value Object: Represents aggregated power position for a trading day.
    /// </summary>
    public sealed record PowerPosition
    {
        /// <summary>
        /// The trading day in local timezone.
        /// </summary>
        public DateTime PositionDate { get; private set; }

        /// <summary>
        /// Aggregated volumes by hour (exactly 24 periods).
        /// Index 0: 23:00 (previous day), Index 1: 00:00, ..., Index 23: 22:00
        /// </summary>
        public IReadOnlyList<PeriodVolume> PeriodVolumes { get; private set; }

        /// <summary>
        /// When this aggregation was computed (local time in configured timezone).
        /// </summary>
        public DateTime ExtractionTime { get; private set; }

        /// <summary>
        /// The timezone used for aggregation (e.g., "GMT Standard Time").
        /// </summary>
        public string TimeZoneId { get; private set; }

        /// <summary>
        /// Constructor - enforces invariants.
        /// </summary>
        public PowerPosition(
            DateTime positionDate,
            IReadOnlyList<PeriodVolume> periodVolumes,
            DateTime extractionTime,
            string timeZoneId)
        {
            if (periodVolumes == null)
                throw new ArgumentNullException(nameof(periodVolumes));

            if (periodVolumes.Count != 24)
                throw new ArgumentException($"Must have exactly 24 period volumes, got {periodVolumes.Count}", nameof(periodVolumes));

            if (string.IsNullOrWhiteSpace(timeZoneId))
                throw new ArgumentException("TimeZoneId cannot be null or empty.", nameof(timeZoneId));

            PositionDate = positionDate;
            PeriodVolumes = periodVolumes;
            ExtractionTime = extractionTime;
            TimeZoneId = timeZoneId;
        }

        /// <summary>
        /// Generates CSV filename: PowerPosition_YYYYMMDD_HHMM.csv
        /// Format received from configuration options.
        /// </summary>
        public string GetFileName(string csvFileNameFormat)
        {
            var dateStr = PositionDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var timeStr = ExtractionTime.ToString("HHmm", CultureInfo.InvariantCulture);
            return string.Format(csvFileNameFormat, dateStr, timeStr);
        }

        /// <summary>
        /// Generates CSV lines: header + 24 data rows.
        /// Format: "Local Time,Volume"
        /// </summary>
        public IEnumerable<string> ToCsvLines()
        {
            yield return "Local Time,Volume";

            foreach (var volume in PeriodVolumes)
            {
                var timeStr = volume.LocalTime.ToString("HH:mm", CultureInfo.InvariantCulture);
                var volumeStr = volume.AggregatedVolume.ToString(CultureInfo.InvariantCulture);
                yield return $"{timeStr},{volumeStr}";
            }
        }
    }
}
