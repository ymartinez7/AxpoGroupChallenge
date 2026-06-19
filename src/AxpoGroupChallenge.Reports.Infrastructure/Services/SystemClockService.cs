using AxpoGroupChallenge.Reports.Application.Interfaces;

namespace AxpoGroupChallenge.Reports.Infrastructure.Services
{
    public sealed class SystemClockService : IClockService
    {
        public DateTime GetNow(string timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                throw new ArgumentException("TimeZoneId cannot be null or empty.", nameof(timeZoneId));

            try
            {
                var utcNow = DateTime.UtcNow;
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.Utc, tz);
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new ArgumentException($"Invalid timezone ID: {timeZoneId}", nameof(timeZoneId), ex);
            }
        }

        public DateTime GetToday(string timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                throw new ArgumentException("TimeZoneId cannot be null or empty.", nameof(timeZoneId));

            var now = GetNow(timeZoneId);
            return new DateTime(now.Year, now.Month, now.Day);
        }
    }
}
