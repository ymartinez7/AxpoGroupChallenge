using AxpoGroupChallenge.Reports.Application;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Host.Configurations;
using AxpoGroupChallenge.Reports.Host.Interfaces;
using AxpoGroupChallenge.Reports.Host.Workers;
using AxpoGroupChallenge.Reports.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace AxpoGroupChallenge.Reports.Host
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.Configure<WorkerExecutionOptions>(context.Configuration.GetSection("WorkerExecutionOptions"));
                    services.Configure<ReportFileOptions>(context.Configuration.GetSection("ReportFileOptions"));

                    services.AddScoped<IPeriodicExecutor>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<PeriodicExecutor>>();
                        var options = provider.GetRequiredService<IOptions<WorkerExecutionOptions>>();
                        var interval = TimeSpan.FromMinutes(options.Value.ExtractionIntervalMinutes);

                        return new PeriodicExecutor(logger, interval);
                    });

                    services.AddApplicationServices();
                    services.AddInfrastructureServices();
                    services.AddHostedService<PowerPositionReportWorker>();
                })
                .UseSerilog((context, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
