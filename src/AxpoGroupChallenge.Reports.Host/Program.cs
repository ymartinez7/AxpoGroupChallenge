using AxpoGroupChallenge.Reports.Application;
using AxpoGroupChallenge.Reports.Application.Configurations;
using AxpoGroupChallenge.Reports.Host.Configurations;
using AxpoGroupChallenge.Reports.Host.Workers;
using AxpoGroupChallenge.Reports.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorkerExecutionOptions>(context.Configuration.GetSection("WorkerExecutionOptions"));
        services.Configure<ReportFileOptions>(context.Configuration.GetSection("ReportFileOptions"));

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
