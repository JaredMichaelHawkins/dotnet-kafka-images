using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace Shared.Extensions;

public static class KafkaExtensions
{

    public static IProducer<string, byte[]> CreateProducer(
        IConfiguration configuration)
    {
        return new ProducerBuilder<string, byte[]> (
            new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:ProducerSettings:BootstrapServers"]
            }).Build();
    }

    public static IConsumer<Ignore, string> CreateConsumer(
        IConfiguration configuration)
    {
        return new ConsumerBuilder<Ignore, string>(
            new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:ConsumerSettings:BootstrapServers"],
                GroupId = configuration["Kafka:ConsumerSettings:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).Build();
    }
}
