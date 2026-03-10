using ConversionReports.Application.Options;
using ConversionReports.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConversionReports.Infrastructure.HostedServices;

public class BatchProcessingHostedService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly BatchingOptions _options;
	private readonly ILogger<BatchProcessingHostedService> _logger;

	public BatchProcessingHostedService(
		IServiceScopeFactory scopeFactory,
		IOptions<BatchingOptions> options,
		ILogger<BatchProcessingHostedService> logger)
	{
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.Yield();

		using var timer = new PeriodicTimer(_options.ProcessingInterval);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await using var scope = _scopeFactory.CreateAsyncScope();

				var processor = scope.ServiceProvider.GetRequiredService<ReportBatchProcessor>();
				var processedCount = await processor.ProcessDueBatchesAsync(stoppingToken);

				if (processedCount > 0)
				{
					_logger.LogInformation("Processed {ProcessedCount} report batches", processedCount);
				}
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Unexpected error while processing report batches");
			}

			var hasNextTick = await timer.WaitForNextTickAsync(stoppingToken);

			if (!hasNextTick)
			{
				break;
			}
		}
	}
}
