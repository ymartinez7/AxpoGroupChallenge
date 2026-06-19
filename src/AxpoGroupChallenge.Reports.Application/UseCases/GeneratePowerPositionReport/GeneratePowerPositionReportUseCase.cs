using Axpo;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport
{
    public sealed class GeneratePowerPositionReportUseCase(
        ILogger<GeneratePowerPositionReportUseCase> logger,
        IOptions<ReportFileOptions> options,
        IPowerService powerService,
        IPowerTradeAgregatorService powerTradeAgregatorService,
        IFileExportService exportService,
        IClockService clockService) : IGeneratePowerPositionReportUseCase
    {
        private readonly ILogger<GeneratePowerPositionReportUseCase> _logger = logger;
        private readonly ReportFileOptions _options = options.Value;
        private readonly IPowerService _powerService = powerService;
        private readonly IPowerTradeAgregatorService powerTradeAgregatorService = powerTradeAgregatorService;
        private readonly IFileExportService _exportService = exportService;
        private readonly IClockService _clockService = clockService;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var extractionTime = _clockService.GetNow(_options.TimeZone);
                var tradeDate = _clockService.GetToday(_options.TimeZone);
                var powerTrades = _powerService.GetTrades(tradeDate).ToList();

                if (powerTrades.Count == 0)
                {
                    _logger.LogWarning("No trades found for {TradeDate}. PowerService returned empty result.", tradeDate);
                    return;
                }

                var periodVolumes = powerTradeAgregatorService.AggregatePeriods(powerTrades);

                var fileExportRequest = new FileExportRequest(
                                    periodVolumes,
                                    tradeDate,
                                    extractionTime,
                                    _options.OutputDirectoryPath,
                                    _options.FileNameFormat,
                                    _options.TimeZone);

                await _exportService.ExportAsync(fileExportRequest, cancellationToken);

                _logger.LogInformation("Extraction complete in {DurationMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Extraction failed: {ErrorMessage}", ex.Message);
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
