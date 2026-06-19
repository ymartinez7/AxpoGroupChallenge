namespace AxpoGroupChallenge.Reports.Host.Interfaces
{
    public interface IPeriodicExecutor
    {
        Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
    }
}
