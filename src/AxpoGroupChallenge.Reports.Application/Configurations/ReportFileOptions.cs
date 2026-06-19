namespace AxpoGroupChallenge.Reports.Application.Configurations
{
    public sealed record ReportFileOptions
    {
        public string OutputDirectoryPath { get; init; } = null!;
        public string TimeZone { get; init; } = null!;
        public string FileNameFormat { get; init; } = null!;
    }
}
