namespace AxpoGroupChallenge.Reports.Host.Configurations
{
    public sealed record WorkerExecutionOptions
    {
        public int ExtractionIntervalMinutes { get; init; }
    }
}
