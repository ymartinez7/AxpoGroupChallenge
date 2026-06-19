namespace AxpoGroupChallenge.Reports.Application.DTOs
{
    public sealed record FileExportRequest(
        IReadOnlyDictionary<int, decimal> Trades,
        DateTime TradeDate,
        DateTime ExtractionTime,
        string OutputDirectoryPath,
        string FileNameFormat,
        string TimeZone);
}
