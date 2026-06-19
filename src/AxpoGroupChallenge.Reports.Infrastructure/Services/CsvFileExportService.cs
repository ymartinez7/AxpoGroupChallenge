using AxpoGroupChallenge.Reports.Application.DTOs;
using AxpoGroupChallenge.Reports.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace AxpoGroupChallenge.Reports.Infrastructure.Services
{
    public sealed class CsvFileExportService(ILogger<CsvFileExportService> logger) : IFileExportService
    {
        private readonly ILogger<CsvFileExportService> _logger = logger;

        public async Task ExportAsync(
            FileExportRequest request, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Trades);

            if (string.IsNullOrWhiteSpace(request.OutputDirectoryPath))
                throw new ArgumentException("Output directory path cannot be null or empty.", nameof(request.OutputDirectoryPath));

            try
            {
                Directory.CreateDirectory(request.OutputDirectoryPath);

                var csvLines = GenerateCsvLines(request.Trades, request.TradeDate);
                var fileName = GenerateFileName(request.TradeDate, request.ExtractionTime, request.FileNameFormat);
                var filePath = Path.Combine(request.OutputDirectoryPath, fileName);

                await File.WriteAllLinesAsync(filePath, csvLines, cancellationToken);

                _logger.LogInformation(
                    "Exported {FileName} with {RowCount} rows from {TradeCount} trades",
                    fileName,
                    csvLines.Count,
                    request.Trades.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export CSV");
                throw;
            }
        }

        private static IReadOnlyList<string> GenerateCsvLines(IReadOnlyDictionary<int, decimal> periodVolumes, DateTime tradeDate)
        {
            var csvLines = new List<string> { "Local Time,Volume" };

            // Period 1 = 23:00 (previous day)
            var vol1 = periodVolumes.TryGetValue(1, out var v1) ? v1 : 0;
            csvLines.Add($"23:00,{vol1.ToString(CultureInfo.InvariantCulture)}");

            // Periods 2-24 = 00:00 to 22:00
            for (int period = 2; period <= 24; period++)
            {
                var hour = period - 2;
                var volume = periodVolumes.TryGetValue(period, out var vol) ? vol : 0;
                var timeStr = new DateTime(tradeDate.Year, tradeDate.Month, tradeDate.Day, hour, 0, 0)
                    .ToString("HH:mm", CultureInfo.InvariantCulture);
                csvLines.Add($"{timeStr},{volume.ToString(CultureInfo.InvariantCulture)}");
            }

            return csvLines;
        }

        private static string GenerateFileName(DateTime tradeDate, DateTime extractionTime, string fileNameFormat)
        {
            var dateStr = tradeDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var timeStr = extractionTime.ToString("HHmm", CultureInfo.InvariantCulture);
            return string.Format(fileNameFormat, dateStr, timeStr);
        }
    }
}
