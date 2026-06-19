namespace AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport
{
    public interface IGeneratePowerPositionReportUseCase
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
