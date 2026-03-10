using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Options;
using ConversionReports.Application.Services;
using ConversionReports.Infrastructure.Caching;
using ConversionReports.Infrastructure.HostedServices;
using ConversionReports.Infrastructure.Messaging;
using ConversionReports.Infrastructure.Options;
using ConversionReports.Infrastructure.Persistence;
using ConversionReports.Infrastructure.Persistence.Repositories;
using ConversionReports.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConversionReports.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(services);

		ArgumentNullException.ThrowIfNull(configuration);

		var connectionString = configuration.GetConnectionString("Postgres");

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException("Postgres connection string is missing");
		}

		services.Configure<BatchingOptions>(configuration.GetSection(BatchingOptions.SectionName));
		services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
		services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

		services.AddMemoryCache();

		services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

		services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
		services.AddScoped<IReportRequestReadRepository, ReportRequestReadRepository>();
		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<IReportResultCache, MemoryReportResultCache>();
		services.AddScoped<IExpensiveReportProvider, DemoExpensiveReportProvider>();

		services.AddScoped<ReportBatchFactory>();
		services.AddScoped<ReportRequestIngestionService>();
		services.AddScoped<ReportBatchProcessor>();

		services.AddHostedService<KafkaReportRequestedConsumerHostedService>();
		services.AddHostedService<BatchProcessingHostedService>();

		return services;
	}
}
