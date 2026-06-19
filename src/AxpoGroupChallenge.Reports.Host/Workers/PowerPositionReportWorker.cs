using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;
using AxpoGroupChallenge.Reports.Host.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AxpoGroupChallenge.Reports.Host.Workers;

public sealed class PowerPositionReportWorker(
    ILogger<PowerPositionReportWorker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerExecutionOptions> options) : BackgroundService
{
    private readonly ILogger<PowerPositionReportWorker> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(options.Value.ExtractionIntervalMinutes);
    private readonly TimeSpan _gracefulShutdownTimeout = TimeSpan.FromSeconds(60);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private PeriodicTimer? _timer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Power position report worker started");
        _timer = new PeriodicTimer(_interval);

        await TryRunExtractionAsync(stoppingToken);

        while (await _timer.WaitForNextTickAsync(stoppingToken))
            await TryRunExtractionAsync(stoppingToken);

        _logger.LogInformation("Periodic execution loop completed");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker stopping");

        _timer?.Dispose();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_gracefulShutdownTimeout);
            await _semaphore.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Graceful shutdown timeout after {TimeoutSeconds}s", _gracefulShutdownTimeout.TotalSeconds);
        }
        finally
        {
            _semaphore.Dispose();
            _logger.LogInformation("Worker stopped");
            await base.StopAsync(cancellationToken);
        }
    }

    private async Task TryRunExtractionAsync(CancellationToken cancellationToken)
    {
        if (!_semaphore.Wait(0))
        {
            _logger.LogWarning("Extraction skipped; previous extraction still running");
            return;
        }

        try
        {
            _logger.LogInformation("Extraction starting");
            await using var scope = _scopeFactory.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IGeneratePowerPositionReportUseCase>();
            await useCase.ExecuteAsync(cancellationToken);
            _logger.LogInformation("Extraction complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction failed: {Message}", ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
