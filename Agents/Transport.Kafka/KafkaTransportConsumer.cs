using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transport.Abstraction;

namespace Transport.Kafka
{
    public class KafkaTransportConsumer : ITransportConsumer
    {
        private readonly ITransportSender _transportSender;
        private readonly ILogger<KafkaTransportConsumer> _logger;
        private readonly KafkaConfiguration _configuration;
        public event ITransportConsumer.ConsumedEventHandler OnConsumed;
        public event ITransportConsumer.RpcRequestEventHandler OnRpcRequest;

        public KafkaTransportConsumer(ITransportSender transportSender, IOptions<KafkaConfiguration> options,
            ILogger<KafkaTransportConsumer> logger)
        {
            _transportSender = transportSender;
            _logger = logger;
            _configuration = options.Value;
        }

        public void Subscribe(string id, string rpcQueueTopic, CancellationToken cancellationToken,
            params string[] queueTopic)
        {
            var config = new ConsumerConfig()
            {
                BootstrapServers = _configuration.Bootstrap,
                Acks = Acks.Leader,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Latest,
                GroupId = id,
            };
            var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(queueTopic);

            var rpcConsumer = new ConsumerBuilder<string, string>(config).Build();
            rpcConsumer.Subscribe(rpcQueueTopic);

            Task.Run(()=>ListenMsg(consumer, cancellationToken), cancellationToken);
            Task.Run(()=>ListenRpc(rpcConsumer, cancellationToken), cancellationToken);

        }

        public void Subscribe(string id, string rpcQueueTopic, params string[] queueTopic)
        {
            Subscribe(id, rpcQueueTopic, CancellationToken.None, queueTopic);
        }

        private async Task ListenMsg(IConsumer<string, string> consumer, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var msg = consumer.Consume(cancellationToken);
                    if (OnConsumed != null) await OnConsumed(msg.Value, msg.Topic);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while processing message");
                }
            }
        }
        private async Task ListenRpc(IConsumer<string, string> consumer,CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var msg = consumer.Consume(cancellationToken);
                    var replayToHeader = msg.Headers.FirstOrDefault(x => x.Key == "ReplayTo")?.GetValueBytes();
                    if (replayToHeader != null)
                    {
                        if (OnRpcRequest != null)
                        {
                            foreach (var inv in OnRpcRequest.GetInvocationList())
                            {
                                var result = await (Task<string>)inv.DynamicInvoke(msg.Value, msg.Topic);
                                if (result != null)
                                {
                                    await _transportSender.SendAsync(Encoding.UTF8.GetString(replayToHeader), result);
                                    break;
                                }
                            }

                          
                        }

                        _logger.LogInformation($"{DateTime.Now} Processed");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while processing rpc request");
                }
            }
        }

        // public ConsumeResult<string> Consume()
        // {
        //     var result = _consumer.Consume();
        //     return new ConsumeResult<string>
        //     {
        //         Result = result.Value,
        //         Headers = result.Headers
        //             .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes()))
        //     };
        // }
        //
        // public ConsumeResult<string> Consume(TimeSpan timeout)
        // {
        //     var result = _consumer.Consume(timeout);
        //     return new ConsumeResult<string>
        //     {
        //         Result = result.Value,
        //         Headers = result.Headers
        //             .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes()))
        //     };
        // }
        //
        // public ConsumeResult<T> Consume<T>()
        // {
        //     var strResult = Consume();
        //     try
        //     {
        //         return new ConsumeResult<T>
        //         {
        //             Headers = strResult.Headers,
        //             Result = JsonSerializer.Deserialize<T>(strResult.Result)
        //         };
        //     }
        //     catch (Exception e)
        //     {
        //         _logger.LogError(e, $"Can`t parse message {strResult.Result}");
        //     }
        //
        //     return null;
        // }
        //
        // public ConsumeResult<T> Consume<T>(TimeSpan timeout)
        // {
        //     var strResult = Consume(timeout);
        //     return new ConsumeResult<T>
        //     {
        //         Headers = strResult.Headers,
        //         Result = JsonSerializer.Deserialize<T>(strResult.Result)
        //     };
        // }
        //
        // public void Commit()
        // {
        //     _consumer.Commit();
        // }

        // public void Subscribe(string id, string rpcQueueTopic, params string[] queueTopic)
        // {
        //     throw new NotImplementedException();
        // }
    }
}