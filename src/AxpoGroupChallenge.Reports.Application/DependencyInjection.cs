using AxpoGroupChallenge.Reports.Application.Interfaces;
using AxpoGroupChallenge.Reports.Application.Services;
using AxpoGroupChallenge.Reports.Application.UseCases.GeneratePowerPositionReport;
using Microsoft.Extensions.DependencyInjection;

namespace AxpoGroupChallenge.Reports.Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IPowerTradeAgregatorService, PowerTradeAgregatorService>();
            services.AddScoped<IGeneratePowerPositionReportUseCase, GeneratePowerPositionReportUseCase>();

            return services;
        }
    }
}
