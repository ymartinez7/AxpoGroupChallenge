using AxpoGroupChallenge.Reports.Application;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Host.Configurations;
using AxpoGroupChallenge.Reports.Host.Workers;
using AxpoGroupChallenge.Reports.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AxpoGroupChallenge.Reports.Host.IntegrationTests.Workers;

public sealed class PowerPositionReportWorkerTests : IDisposable
{
    private readonly string _outputDir;

    public PowerPositionReportWorkerTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task Worker_RunsEnd2End_GeneratesCsvFile()
    {
        using var host = BuildHost();
        await host.StartAsync();

        var found = await WaitForCsvFileAsync(_outputDir, TimeSpan.FromSeconds(10));
        await host.StopAsync();

        Assert.True(found, "No CSV file was generated within the time limit");
        Assert.Single(Directory.GetFiles(_outputDir, "*.csv"));

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

        var reportOptions = Options.Create(new ReportFileOptions
        {
            OutputDirectoryPath = _outputDir,
            TimeZone = "GMT Standard Time",
            FileNameFormat = "PowerPosition_{0:yyyyMMdd}_{1:HHmm}.csv"
        });

        var workerOptions = Options.Create(new WorkerExecutionOptions
        {
            ExtractionIntervalMinutes = 60
        });

        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(reportOptions);
                services.AddSingleton(workerOptions);
                services.AddApplicationServices();
                services.AddInfrastructureServices();
                services.AddHostedService<PowerPositionReportWorker>();
            })
            .Build();
    }

    private static async Task<bool> WaitForCsvFileAsync(string dir, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (Directory.GetFiles(dir, "*.csv").Length > 0)
                return true;
            await Task.Delay(200);
        }
        return false;
    }
}
