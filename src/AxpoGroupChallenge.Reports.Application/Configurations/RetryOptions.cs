namespace AxpoGroupChallenge.Reports.Application.Configurations;

public sealed record RetryOptions
{
    public int MaxRetryAttempts { get; init; }
    public TimeSpan BaseDelay { get; init; }
}
