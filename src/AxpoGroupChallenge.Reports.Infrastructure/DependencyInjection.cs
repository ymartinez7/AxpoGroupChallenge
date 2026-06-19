using Axpo;
using AxpoGroupChallenge.Reports.Application.Interfaces;
using AxpoGroupChallenge.Reports.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AxpoGroupChallenge.Reports.Infrastructure
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IPowerService, PowerService>();
            services.AddScoped<IFileExportService, CsvFileExportService>();
            services.AddSingleton<IClockService, SystemClockService>();

            return services;
        }
    }
}
