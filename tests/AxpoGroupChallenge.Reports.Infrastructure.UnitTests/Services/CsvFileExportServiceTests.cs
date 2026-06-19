using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;
using AxpoGroupChallenge.Reports.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AxpoGroupChallenge.Reports.Infrastructure.UnitTests.Services;

public sealed class CsvFileExportServiceTests : IDisposable
{
    private readonly string _outputDir;
    private readonly CsvFileExportService _csvFileExportService;

    public CsvFileExportServiceTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _csvFileExportService = new(NullLogger<CsvFileExportService>.Instance);
    }

    [Fact]
    public async Task ExportAsync_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _csvFileExportService.ExportAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExportAsync_EmptyOutputPath_ThrowsArgumentException()
    {
        var request = BuildRequest(outputDir: "");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _csvFileExportService.ExportAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ExportAsync_HappyPath_CreatesCsvFile()
    {
        var request = BuildRequest();

        await _csvFileExportService.ExportAsync(request, CancellationToken.None);

        Assert.True(Directory.GetFiles(_outputDir, "*.csv").Length > 0);
    }

    [Fact]
    public async Task ExportAsync_HappyPath_FirstLineIsHeader()
    {
        var request = BuildRequest();

        await _csvFileExportService.ExportAsync(request, CancellationToken.None);

        var file = Directory.GetFiles(_outputDir, "*.csv").Single();
        var firstLine = (await File.ReadAllLinesAsync(file))[0];
        Assert.Equal("Local Time,Volume", firstLine);
    }

    [Fact]
    public async Task ExportAsync_HappyPath_Has25Lines()
    {
        var request = BuildRequest();

        await _csvFileExportService.ExportAsync(request, CancellationToken.None);

        var file = Directory.GetFiles(_outputDir, "*.csv").Single();
        var lines = await File.ReadAllLinesAsync(file);
        Assert.Equal(25, lines.Length);
    }

    [Fact]
    public async Task ExportAsync_FileNameFollowsConfiguredFormat()
    {
        var tradeDate = new DateTime(2024, 6, 1);
        var extractionTime = new DateTime(2024, 6, 1, 14, 30, 0);
        var request = BuildRequest(tradeDate: tradeDate, extractionTime: extractionTime);

        await _csvFileExportService.ExportAsync(request, CancellationToken.None);

        var file = Path.GetFileName(Directory.GetFiles(_outputDir, "*.csv").Single());
        Assert.Equal("PowerPosition_20240601_1430.csv", file);
    }

    private FileExportRequest BuildRequest(
        string? outputDir = null,
        decimal period1Volume = 0m,
        decimal period2Volume = 0m,
        DateTime? tradeDate = null,
        DateTime? extractionTime = null)
    {
        var periods = new Dictionary<int, decimal>();
        for (int i = 1; i <= 24; i++)
            periods[i] = 0m;
        periods[1] = period1Volume;
        periods[2] = period2Volume;

        return new FileExportRequest(
            Trades: periods,
            TradeDate: tradeDate ?? new DateTime(2024, 6, 1),
            ExtractionTime: extractionTime ?? new DateTime(2024, 6, 1, 14, 30, 0),
            OutputDirectoryPath: outputDir ?? _outputDir,
            FileNameFormat: "PowerPosition_{0}_{1}.csv",
            TimeZone: "GMT Standard Time");
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, recursive: true);
    }
}
