using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;

namespace AxpoGroupChallenge.Reports.Application.Interfaces
{
    public interface IFileExportService
    {
        Task ExportAsync(FileExportRequest request, CancellationToken cancellationToken);
    }
}
