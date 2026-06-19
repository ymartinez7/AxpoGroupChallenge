using AxpoGroupChallenge.Reports.Application.DTOs;

namespace AxpoGroupChallenge.Reports.Application.Interfaces
{
    public interface IFileExportService
    {
        Task ExportAsync(FileExportRequest request, CancellationToken cancellationToken);
    }
}
