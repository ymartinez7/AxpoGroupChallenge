using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;
using AxpoGroupChallenge.Reports.Host.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace AxpoGroupChallenge.Reports.Host.Workers;

public sealed class PowerPositionReportWorker(
    ILogger<PowerPositionReportWorker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerExecutionOptions> options) : BackgroundService
{
    private readonly ILogger<PowerPositionReportWorker> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(options.Value.ExtractionIntervalMinutes);
    private readonly TimeSpan _lateThreshold = TimeSpan.FromMinutes(1);

    private readonly Channel<DateTimeOffset> _queue = Channel.CreateUnbounded<DateTimeOffset>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Power position report worker started");

        var consumer = ConsumeQueueAsync(stoppingToken);

        _queue.Writer.TryWrite(DateTimeOffset.UtcNow);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            _queue.Writer.TryWrite(DateTimeOffset.UtcNow);

        _queue.Writer.Complete();
        await consumer;

        _logger.LogInformation("Periodic execution loop completed");
    }

    private async Task ConsumeQueueAsync(CancellationToken cancellationToken)
    {
        await foreach (var scheduledAt in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            var delay = DateTimeOffset.UtcNow - scheduledAt;
            if (delay > _lateThreshold)
                _logger.LogWarning("Extraction running {DelaySeconds}s late (tolerance: {ToleranceSeconds}s)",
                    (int)delay.TotalSeconds, (int)_lateThreshold.TotalSeconds);

            await RunExtractionAsync(cancellationToken);
        }
    }

    private async Task RunExtractionAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Extraction starting");
            await using var scope = _scopeFactory.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IGeneratePowerPositionReportUseCase>();
            await useCase.ExecuteAsync(cancellationToken);
            _logger.LogInformation("Extraction complete");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Extraction cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction failed: {Message}", ex.Message);
        }
    }
}
