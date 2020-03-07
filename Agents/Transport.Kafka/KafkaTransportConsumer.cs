using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transport.Abstraction;

namespace Transport.Kafka
{
    public class KafkaTransportConsumer : ITransportConsumer
    {
        private readonly ILogger<KafkaTransportConsumer> _logger;
        private readonly KafkaConfiguration _configuration;
        private IConsumer<string, string> _consumer;

        public KafkaTransportConsumer(IOptions<KafkaConfiguration> options, ILogger<KafkaTransportConsumer> logger)
        {
            _logger = logger;
            _configuration = options.Value;
        }

        public void Subscribe(string id, params string[] queueTopic)
        {
            var config = new ConsumerConfig()
            {
                BootstrapServers = _configuration.Bootstrap,
                Acks = Acks.Leader,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Latest,
                GroupId = id,
            };
            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _consumer.Subscribe(queueTopic);
        }

        public ConsumeResult<string> Consume()
        {
            var result = _consumer.Consume();
            return new ConsumeResult<string>
            {
                Result = result.Value,
                Headers = result.Headers
                    .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes()))
            };
        }

        public ConsumeResult<string> Consume(TimeSpan timeout)
        {
            var result = _consumer.Consume(timeout);
            return new ConsumeResult<string>
            {
                Result = result.Value,
                Headers = result.Headers
                    .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes()))
            };
        }

        public ConsumeResult<T> Consume<T>()
        {
            var strResult = Consume();
            try
            {
                return new ConsumeResult<T>
                {
                    Headers = strResult.Headers,
                    Result = JsonSerializer.Deserialize<T>(strResult.Result)
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Can`t parse message {strResult.Result}");
            }

            return null;
        }

        public ConsumeResult<T> Consume<T>(TimeSpan timeout)
        {
            var strResult = Consume(timeout);
            return new ConsumeResult<T>
            {
                Headers = strResult.Headers,
                Result = JsonSerializer.Deserialize<T>(strResult.Result)
            };
        }

        public void Commit()
        {
            _consumer.Commit();
        }
    }
}