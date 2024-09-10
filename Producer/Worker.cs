using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Shared.Diagnostics;
using Shared.Extensions;
using Shared.Model;

namespace Producer;

public class Worker(ILogger<Worker> logger, IConfiguration config) : BackgroundService
{
    private static readonly TextMapPropagator TextMapPropagator = Propagators.DefaultTextMapPropagator;
    private readonly ILogger<Worker> _logger = logger;
    private readonly IConfiguration _config = config;
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            // ref:
            // https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples/MicroserviceExample
            using var activity = OpenTelemetryExtensions.CreateActivitySource()
                .StartActivity("Producing");

            var weather = new WeatherForecast(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)],
                "L1 8JQ"
            );

            var topicName = config["Kafka:TopicName"]!;
            var serializedModel = JsonSerializer.Serialize(weather);

            // Semantic convention - OpenTelemetry messaging specification:
            // ref: https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
            var activityName = $"publish {topicName}";

            using var producer = KafkaExtensions.CreateProducer(config);
            using var sendActivity = OpenTelemetryExtensions.CreateActivitySource()
                .StartActivity(activityName, ActivityKind.Producer);

            ActivityContext contextToInject = default;
            if (sendActivity != null)
            {
                contextToInject = sendActivity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            var headers = new Headers();
            TextMapPropagator.Inject(new PropagationContext(contextToInject, Baggage.Current), headers,
                InjectTraceContextIntoHeaders);

            sendActivity?.SetTag(MessageTags.System, "kafka");
            sendActivity?.SetTag(MessageTags.DestinationTopic, topicName);
            sendActivity?.SetTag(MessageTags.Operation, "publish");
            sendActivity?.SetTag(MessageTags.Id, weather.Id);
            sendActivity?.SetTag(MessageTags.ClientId,
                $"Producer-{Environment.MachineName}");
            var byteArray = System.Text.Encoding.Default.GetBytes(serializedModel);
            var result = producer.ProduceAsync(topicName,
                new Message<string, byte[]>
                    { Key = weather.Id.ToString(), Value = byteArray, Headers = headers }).Result;

            logger.LogInformation("Kafka - Sending to topic {topicName} completed", topicName);

            await Task.Delay(1000, stoppingToken);
        }
    }

    private void InjectTraceContextIntoHeaders(Headers headers, string key, string value)
    {
        try
        {
            headers.Add(key, Encoding.UTF8.GetBytes(value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to inject trace context.");
        }
    }
}


