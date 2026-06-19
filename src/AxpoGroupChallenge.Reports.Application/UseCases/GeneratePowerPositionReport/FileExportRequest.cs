namespace AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport
{
    public sealed record FileExportRequest(
        IReadOnlyDictionary<int, decimal> Trades,
        DateTime TradeDate,
        DateTime ExtractionTime,
        string OutputDirectoryPath,
        string FileNameFormat,
        string TimeZone);
}
