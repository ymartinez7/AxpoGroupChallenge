using Axpo;
using AxpoGroupChallenge.Reports.Application;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;
using AxpoGroupChallenge.Reports.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace AxpoGroupChallenge.Reports.Host.IntegrationTests.Workers;

public sealed class PowerPositionReportWorkerTests : IDisposable
{
    private readonly string _outputDir;
    private readonly Mock<IPowerService> _powerService;

    public PowerPositionReportWorkerTests()
    {
        _powerService = new();
        _outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var trade = PowerTrade.Create(DateTime.Today, 24);
        _powerService.Setup(x => x.GetTrades(It.IsAny<DateTime>())).Returns([trade]);
    }

    [Fact]
    public async Task UseCase_RunsEnd2End_GeneratesCsvFile()
    {
        using var host = BuildHost();
        var useCase = host.Services.GetRequiredService<IGeneratePowerPositionReportUseCase>();

        await useCase.ExecuteAsync(CancellationToken.None);

        var files = Directory.GetFiles(_outputDir, "*.csv");
        Assert.Single(files);
    }

    [Fact]
    public async Task UseCase_GeneratedCsv_HasHeaderAnd24DataRows()
    {
        using var host = BuildHost();
        var useCase = host.Services.GetRequiredService<IGeneratePowerPositionReportUseCase>();

        await useCase.ExecuteAsync(CancellationToken.None);

        var file = Directory.GetFiles(_outputDir, "*.csv").Single();
        var lines = await File.ReadAllLinesAsync(file);
        Assert.Equal(25, lines.Length);
        Assert.Equal("Local Time,Volume", lines[0]);
    }

    [Fact]
    public async Task Host_StartsAndStops_WithoutException()
    {
        using var host = BuildHost();

        var exception = await Record.ExceptionAsync(async () =>
        {
            await host.StartAsync();
            await host.StopAsync();
        });

        Assert.Null(exception);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, recursive: true);
    }

    private IHost BuildHost()
    {
        Directory.CreateDirectory(_outputDir);

        var options = Options.Create(new ReportFileOptions
        {
            OutputDirectoryPath = _outputDir,
            TimeZone = "GMT Standard Time",
            FileNameFormat = "PowerPosition_{0}_{1}.csv"
        });

        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(options);
                services.AddApplicationServices();
                services.AddInfrastructureServices();
                services.AddSingleton<IPowerService>(_powerService.Object);
            })
            .Build();
    }
}
