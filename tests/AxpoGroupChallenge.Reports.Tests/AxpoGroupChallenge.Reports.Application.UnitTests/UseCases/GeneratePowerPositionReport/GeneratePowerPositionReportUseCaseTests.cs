using Axpo;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Application.DTOs;
using AxpoGroupChallenge.Reports.Application.Interfaces;
using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AxpoGroupChallenge.Reports.Application.UnitTests.UseCases.GeneratePowerPositionReport;

public sealed class GeneratePowerPositionReportUseCaseTests
{
    private readonly Mock<IPowerService> _powerServiceMock;
    private readonly Mock<IFileExportService> _exportServiceMock;
    private readonly Mock<IClockService> _clockServiceMock;
    private readonly Mock<IPowerTradeAgregatorService> _agregatorMock;

    private readonly ReportFileOptions _options = new()
    {
        OutputDirectoryPath = Path.GetTempPath(),
        TimeZone = "GMT Standard Time",
        FileNameFormat = "PowerPosition_{0}_{1}.csv"
    };

    public GeneratePowerPositionReportUseCaseTests()
    {
        _powerServiceMock = new();
        _exportServiceMock = new();
        _clockServiceMock = new();
        _agregatorMock = new();
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_CallsExportOnce()
    {
        var tradeDate = new DateTime(2024, 6, 1);
        var extractionTime = new DateTime(2024, 6, 1, 10, 0, 0);
        var trade = PowerTrade.Create(tradeDate, 24);

        _clockServiceMock
            .Setup(x => x.GetToday(_options.TimeZone))
            .Returns(tradeDate);

        _clockServiceMock
            .Setup(x => x.GetNow(_options.TimeZone))
            .Returns(extractionTime);

        _powerServiceMock
            .Setup(x => x.GetTrades(tradeDate))
            .Returns([trade]);

        _agregatorMock
            .Setup(x => x.AggregatePeriods(It.IsAny<IEnumerable<PowerTrade>>()))
            .Returns(new Dictionary<int, decimal> { [1] = 100m });

        await BuildSut().ExecuteAsync(CancellationToken.None);

        _exportServiceMock.Verify(x => x.ExportAsync(
            It.IsAny<FileExportRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyTradeList_DoesNotCallExport()
    {
        var tradeDate = DateTime.Today;

        _clockServiceMock
            .Setup(x => x.GetToday(_options.TimeZone))
            .Returns(tradeDate);

        _clockServiceMock
            .Setup(x => x.GetNow(_options.TimeZone))
            .Returns(DateTime.Now);

        _powerServiceMock
            .Setup(x => x.GetTrades(tradeDate))
            .Returns([]);

        await BuildSut().ExecuteAsync(CancellationToken.None);

        _exportServiceMock.Verify(x => x.ExportAsync(
            It.IsAny<FileExportRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExportThrows_DoesNotPropagate()
    {
        var tradeDate = DateTime.Today;
        var trade = PowerTrade.Create(tradeDate, 24);

        _clockServiceMock
            .Setup(x => x.GetToday(_options.TimeZone))
            .Returns(tradeDate);

        _clockServiceMock
            .Setup(x => x.GetNow(_options.TimeZone))
            .Returns(DateTime.Now);

        _powerServiceMock
            .Setup(x => x.GetTrades(tradeDate))
            .Returns([trade]);

        _agregatorMock
            .Setup(x => x.AggregatePeriods(It.IsAny<IEnumerable<PowerTrade>>()))
            .Returns(new Dictionary<int, decimal> { [1] = 50m });

        _exportServiceMock.Setup(x => x.ExportAsync(
                It.IsAny<FileExportRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk full"));

        var exception = await Record.ExceptionAsync(() =>
            BuildSut().ExecuteAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    private GeneratePowerPositionReportUseCase BuildSut() => new(
        NullLogger<GeneratePowerPositionReportUseCase>.Instance,
        Options.Create(_options),
        _powerServiceMock.Object,
        _agregatorMock.Object,
        _exportServiceMock.Object,
        _clockServiceMock.Object);
}
