using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;
using AxpoGroupChallenge.Reports.Host.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AxpoGroupChallenge.Reports.Host.Workers
{
    public sealed class PowerPositionReportWorker(
        ILogger<PowerPositionReportWorker> logger,
        IGeneratePowerPositionReportUseCase useCase,
        IPeriodicExecutor executor) : BackgroundService
    {
        private readonly ILogger<PowerPositionReportWorker> _logger = logger;
        private readonly IGeneratePowerPositionReportUseCase _useCase = useCase;
        private readonly IPeriodicExecutor _executor = executor;
        private readonly TimeSpan _gracefulShutdownTimeout = TimeSpan.FromSeconds(60);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Power position report worker started at {Datetime}", DateTime.Now.ToString());
            await _executor.ExecuteAsync(_useCase.ExecuteAsync, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping");

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_gracefulShutdownTimeout);

                if (_executor is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Graceful shutdown timeout after {TimeoutSeconds}s", _gracefulShutdownTimeout.TotalSeconds);
            }
            finally
            {
                _logger.LogInformation("Worker stopped");
                await base.StopAsync(cancellationToken);
            }
        }
    }
}
