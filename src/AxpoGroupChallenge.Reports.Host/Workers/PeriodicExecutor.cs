using AxpoGroupChallenge.Reports.Host.Interfaces;
using Microsoft.Extensions.Logging;

namespace AxpoGroupChallenge.Reports.Host.Workers
{
    public sealed class PeriodicExecutor(
        ILogger<PeriodicExecutor> logger,
        TimeSpan interval) : IPeriodicExecutor, IAsyncDisposable
    {
        private readonly ILogger<PeriodicExecutor> _logger = logger;
        private readonly TimeSpan _interval = interval;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private PeriodicTimer? _timer;

        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            try
            {
                _timer = new PeriodicTimer(_interval);
                await ExecuteInitialAsync(action, cancellationToken);
                await ExecutePeriodicAsync(action, cancellationToken);

                _logger.LogInformation("Periodic execution loop completed");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Periodic execution cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Periodic execution fatal error");
            }
        }

        private async Task ExecuteInitialAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initial execution starting");

            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await action(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial execution failed");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ExecutePeriodicAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            while (_timer != null && await _timer.WaitForNextTickAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (await _semaphore.WaitAsync(0))
                {
                    try
                    {
                        _logger.LogInformation("Executing periodic action");
                        await action(cancellationToken);
                        _logger.LogInformation("Periodic action completed");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Execution cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Execution exception: {Message}", ex.Message);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                else
                {
                    _logger.LogWarning("Execution skipped; previous execution still running");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _timer?.Dispose();
            _semaphore?.Dispose();
        }
    }
}
