using Confluent.Kafka;
using Confluent.Kafka.Admin;
using ConversionReports.Application.Exceptions;
using ConversionReports.Application.Services;
using ConversionReports.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConversionReports.Infrastructure.Messaging;

public class KafkaReportRequestedConsumerHostedService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly KafkaOptions _options;
	private readonly ILogger<KafkaReportRequestedConsumerHostedService> _logger;

	public KafkaReportRequestedConsumerHostedService(
		IServiceScopeFactory scopeFactory,
		IOptions<KafkaOptions> options,
		ILogger<KafkaReportRequestedConsumerHostedService> logger)
	{
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.Yield();

		await EnsureTopicExistsWithRetryAsync(stoppingToken);

		var consumerConfig = new ConsumerConfig
		{
			BootstrapServers = _options.BootstrapServers,
			GroupId = _options.ConsumerGroupId,
			EnableAutoCommit = false,
			AllowAutoCreateTopics = false,
			AutoOffsetReset = MapAutoOffsetReset(_options.AutoOffsetReset)
		};

		using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

		consumer.Subscribe(_options.TopicName);

		_logger.LogInformation(
			"Kafka consumer subscribed to topic {TopicName} using bootstrap servers {BootstrapServers}",
			_options.TopicName,
			_options.BootstrapServers);

		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				ConsumeResult<Ignore, string>? consumeResult = null;

				try
				{
					consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

					if (consumeResult is null || consumeResult.Message?.Value is null)
					{
						continue;
					}

					var message = KafkaMessageSerializer.Deserialize(consumeResult.Message.Value);

					await using var scope = _scopeFactory.CreateAsyncScope();
					var service = scope.ServiceProvider.GetRequiredService<ReportRequestIngestionService>();

					await service.HandleAsync(message, stoppingToken);
					consumer.Commit(consumeResult);
				}
				catch (RequestConflictException exception)
				{
					_logger.LogWarning(
						exception,
						"Conflicting request detected for request {RequestId} with existing user {ExistingUserId} and incoming user {IncomingUserId}",
						exception.RequestId,
						exception.ExistingUserId,
						exception.IncomingUserId);

					if (consumeResult is not null)
					{
						consumer.Commit(consumeResult);
					}
				}
				catch (ConsumeException exception)
				{
					_logger.LogError(exception, "Kafka consume error while reading report request messages");
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					break;
				}
				catch (Exception exception)
				{
					_logger.LogError(
						exception,
						"Failed to handle Kafka message at topic {Topic}, partition {Partition}, offset {Offset}",
						consumeResult?.Topic,
						consumeResult?.Partition.Value,
						consumeResult?.Offset.Value);
				}
			}
		}
		finally
		{
			consumer.Close();
		}
	}

	private async Task EnsureTopicExistsWithRetryAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				using var adminClient = new AdminClientBuilder(new AdminClientConfig
				{
					BootstrapServers = _options.BootstrapServers
				}).Build();

				await adminClient.CreateTopicsAsync(
				[
					new TopicSpecification
					{
						Name = _options.TopicName,
						NumPartitions = 1,
						ReplicationFactor = 1
					}
				]);

				_logger.LogInformation("Kafka topic {TopicName} is ready", _options.TopicName);

				return;
			}
			catch (CreateTopicsException exception) when (exception.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
			{
				_logger.LogInformation("Kafka topic {TopicName} already exists", _options.TopicName);

				return;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception exception)
			{
				_logger.LogWarning(exception, "Kafka topic {TopicName} is not ready yet", _options.TopicName);
				await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
			}
		}
	}

	private static AutoOffsetReset MapAutoOffsetReset(AutoOffsetResetBehavior value)
	{
		return value switch
		{
			AutoOffsetResetBehavior.Earliest => AutoOffsetReset.Earliest,
			AutoOffsetResetBehavior.Latest => AutoOffsetReset.Latest,
			_ => AutoOffsetReset.Earliest
		};
	}
}
