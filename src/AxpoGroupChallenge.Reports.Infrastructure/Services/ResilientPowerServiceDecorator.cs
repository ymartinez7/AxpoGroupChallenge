using Axpo;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace AxpoGroupChallenge.Reports.Infrastructure.Services;

public sealed class ResilientPowerServiceDecorator(
    IPowerService inner,
    IOptions<RetryOptions> options,
    ILogger<ResilientPowerServiceDecorator> logger) : IPowerService
{
    private readonly IPowerService _inner = inner;
    private readonly ILogger<ResilientPowerServiceDecorator> _logger = logger;
    private readonly ResiliencePipeline<IEnumerable<PowerTrade>> _pipeline = BuildPipeline(options.Value, logger);

    public IEnumerable<PowerTrade> GetTrades(DateTime date)
        => _pipeline.Execute(() => _inner.GetTrades(date));

    public Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date)
        => _inner.GetTradesAsync(date);

    private static ResiliencePipeline<IEnumerable<PowerTrade>> BuildPipeline(
        RetryOptions options,
        ILogger logger)
    {
        if (options.MaxRetryAttempts <= 0)
            return ResiliencePipeline<IEnumerable<PowerTrade>>.Empty;

        return new ResiliencePipelineBuilder<IEnumerable<PowerTrade>>()
            .AddRetry(new RetryStrategyOptions<IEnumerable<PowerTrade>>
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = options.BaseDelay,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "PowerService.GetTrades failed (attempt {Attempt}/{Max}): {Error}",
                        args.AttemptNumber + 1,
                        options.MaxRetryAttempts,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
