using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transport.Abstraction;

namespace Transport.Kafka
{
    public class KafkaTransportSender : ITransportSender
    {
        private readonly ILogger<KafkaTransportSender> _logger;
        private readonly KafkaConfiguration _kafkaConfiguration;

        public KafkaTransportSender(IOptions<KafkaConfiguration> kafkaConfiguration,
            ILogger<KafkaTransportSender> logger)
        {
            _logger = logger;
            _kafkaConfiguration = kafkaConfiguration.Value;
        }

        public async Task<bool> SendAsync(string receiver, string data, Dictionary<string, string> headers = null)
        {
            try
            {
                var config = new ProducerConfig {BootstrapServers = _kafkaConfiguration.Bootstrap};

                var kHeaders = new Headers();
                headers?.Select(x => new Header(x.Key, Encoding.UTF8.GetBytes(x.Value)))
                    .ToList()
                    .ForEach(x=>kHeaders.Add(x));
                using var producer = new ProducerBuilder<string, string>(config).Build();
                await producer.ProduceAsync(receiver, new Message<string, string>()
                {
                    Value = data,
                    Headers = kHeaders
                });

                producer.Flush(TimeSpan.FromSeconds(10));
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t send to kafka");
                return false;
            }
        }

        public async ValueTask<bool> SendAsync<T>(string receiver, T data, Dictionary<string, string> headers = null)
        {
            return await SendAsync(receiver, JsonSerializer.Serialize(data), headers);
        }


        public async Task<string> CallServiceAsync(string receiver, string data, TimeSpan timeOut)
        {
            var responseTo = $"rpc_response_{Guid.NewGuid()}";
            var adminClient = new AdminClientBuilder(new AdminClientConfig
                {BootstrapServers = _kafkaConfiguration.Bootstrap}).Build();
            try
            {
                var config = new ConsumerConfig()
                {
                    BootstrapServers = _kafkaConfiguration.Bootstrap,
                    Acks = Acks.All,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    GroupId = Guid.NewGuid().ToString(),
                };
                var consumer = new ConsumerBuilder<string, string>(config).Build();
                try
                {
                    await adminClient.CreateTopicsAsync(new[]
                    {
                        new TopicSpecification {Name = responseTo, ReplicationFactor = 1, NumPartitions = 1}
                    });
                }
                catch (CreateTopicsException e)
                {
                    _logger.LogError(e,
                        $"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
                }

                consumer.Subscribe(responseTo);
                _logger.LogInformation($"{DateTime.Now} Sended rpc request");
                await SendAsync(receiver, data, new Dictionary<string, string>
                {
                    ["ReplayTo"] = responseTo
                });
                var response = consumer.Consume(timeOut);
                _logger.LogInformation($"{DateTime.Now} Consumed rpc response");

                consumer.Unsubscribe();
                return response?.Message?.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Rpc error");
            }
            finally
            {
                try
                {
                    await adminClient.DeleteTopicsAsync(new List<string> {responseTo});
                }
                catch (DeleteTopicsException e)
                {
                    _logger.LogError(e, $"Can`t delete topic: {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
                }
            }

            return null;
        }

        public Task<string> CallServiceAsync(string receiver, string data)
        {
            return CallServiceAsync(receiver, data, TimeSpan.FromSeconds(30));
        }

        public async ValueTask<TResp> CallServiceAsync<TReq, TResp>(string receiver, TReq data)
        {
            var result = await CallServiceAsync(receiver, JsonSerializer.Serialize(data));
            if (!string.IsNullOrEmpty(result))
            {
                return JsonSerializer.Deserialize<TResp>(result);
            }

            return default;
        }

        public async ValueTask<TResp> CallServiceAsync<TReq, TResp>(string receiver, TReq data, TimeSpan timeOut)
        {
            var result = await CallServiceAsync(receiver, JsonSerializer.Serialize(data), timeOut);
            if (!string.IsNullOrEmpty(result))
            {
                return JsonSerializer.Deserialize<TResp>(result);
            }

            return default;
        }
        
        
    }
}