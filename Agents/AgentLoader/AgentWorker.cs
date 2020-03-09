using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using AgentLoader.Models;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transport.Abstraction;

namespace AgentLoader
{
    public class AgentWorker : BackgroundService
    {
        private readonly List<AgentAbstract> _agents;
        private readonly ITransportConsumer _consumer;
        private readonly ILogger<AgentWorker> _logger;
        private readonly Configuration _configuration;

        public AgentWorker(IAgentProvider agentProvider,
            ITransportConsumer consumer,
            IOptions<Configuration> configuration,
            ILogger<AgentWorker> logger)
        {
            _agents = agentProvider.Agents;
            _consumer = consumer;
            _logger = logger;
            _configuration = configuration.Value;
            // foreach (var agent in agents)
            // {
            //     agent.SendMessageAsync = (msg) => SendMessageAsync(msg);
            //     agent.CallAsync = CallAsync;
            // }
        }

        // private async Task SendMessageAsync(AgentMessage message, string topic = null, Headers headers = null)
        // {
        //     var config = new ProducerConfig {BootstrapServers = _configuration.KafkaBootstrap};
        //
        //     using var producer = new ProducerBuilder<string, string>(config).Build();
        //     await producer.ProduceAsync(topic ?? message.MessageType.ToString(), new Message<string, string>()
        //     {
        //         Key = message.Author.Type.ToString(),
        //         Value = JsonSerializer.Serialize(message),
        //         Headers = headers
        //     });
        //
        //     producer.Flush(TimeSpan.FromSeconds(10));
        // }
        //
        // private async Task<AgentMessage> CallAsync(AgentMessage<RpcRequest> message, TimeSpan timeout)
        // {
        //     var responseTo = $"rpc_response_{Guid.NewGuid()}";
        //     var adminClient = new AdminClientBuilder(new AdminClientConfig
        //         {BootstrapServers = _configuration.KafkaBootstrap}).Build();
        //     try
        //     {
        //         var config = new ConsumerConfig()
        //         {
        //             BootstrapServers = _configuration.KafkaBootstrap,
        //             Acks = Acks.All,
        //             AutoOffsetReset = AutoOffsetReset.Earliest,
        //             GroupId = message.Author.Type + "_" + message.Author.SubType,
        //             HeartbeatIntervalMs = 500,
        //             StatisticsIntervalMs = 500,
        //             CoordinatorQueryIntervalMs = 500,
        //             AutoCommitIntervalMs = 500,
        //             TopicMetadataRefreshIntervalMs = 500,
        //             TopicMetadataRefreshFastIntervalMs = 500,
        //             
        //         };
        //         var consumer = new ConsumerBuilder<string, string>(config).Build();
        //         try
        //         {
        //             await adminClient.CreateTopicsAsync(new[]
        //             {
        //                 new TopicSpecification {Name = responseTo, ReplicationFactor = 1, NumPartitions = 1}
        //             });
        //         }
        //         catch (CreateTopicsException e)
        //         {
        //             _logger.LogError(e,
        //                 $"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
        //         }
        //
        //         consumer.Subscribe(responseTo);
        //         message.MessageType = MessageType.RpcRequest;
        //         _logger.LogInformation($"{DateTime.Now} Sended rpc request");
        //         await SendMessageAsync(message, headers: new Headers
        //         {
        //             new Header("ReplayTo", Encoding.UTF8.GetBytes(responseTo))
        //         });
        //         var response = consumer.Consume(timeout);
        //         _logger.LogInformation($"{DateTime.Now} Consumed rpc response");
        //
        //         consumer.Unsubscribe();
        //         var msg = JsonSerializer.Deserialize<AgentMessage>(response.Message.Value);
        //         return msg;
        //     }
        //     catch (Exception e)
        //     {
        //         _logger.LogError(e, "Rpc error");
        //     }
        //     finally
        //     {
        //         try
        //         {
        //             await adminClient.DeleteTopicsAsync(new List<string> {responseTo});
        //         }
        //         catch (DeleteTopicsException e)
        //         {
        //             _logger.LogError(e, $"Can`t delete topic: {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
        //         }
        //     }
        //
        //     return null;
        // }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = _agents.Select(x => Task.Run(() => AgentExecutable(x, stoppingToken), stoppingToken))
                .ToArray();
            return Task.WhenAll(tasks);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var agent in _agents)
                {
                    agent.Dispose();
                }

                await base.StopAsync(cancellationToken);
            }
            catch (Exception e) when (e ! is OperationCanceledException)
            {
                Console.WriteLine(e);
            }
        }


        private void AgentExecutable(AgentAbstract agent, CancellationToken stoppingToken)
        {
            agent.StoppingToken = stoppingToken;
            var agentSupported = agent.SupportedMessage.Select(x => x.ToString())
                .ToArray();
            _consumer.Subscribe(agent.Type + "_" + agent.SubType, agent.RpcMessageType.ToString(), stoppingToken,
                agentSupported
            );
            _consumer.OnConsumed += async (result, topic) =>
            {
                if (agentSupported.Contains(topic))
                {
                    var msg = JsonSerializer.Deserialize<AgentMessage>(result);
                    if (msg.Author.Id != agent.Id)
                    {
                        agent.State = AgentState.InWork;
                        if (msg.MessageType != MessageType.Connection)
                            _logger.LogInformation($"Consumed message");
                        await agent.ProcessMessageAsync(msg);
                        agent.State = AgentState.Online;
                    }
                }
            };
            _consumer.OnRpcRequest += async (result, topic) =>
            {
                try
                {
                    if (agent.RpcMessageType.ToString() == topic)
                    {
                        return JsonSerializer.Serialize(
                            await agent.ProcessRpcAsync(JsonSerializer.Deserialize<AgentMessage>(result)
                                .To<RpcRequest>()));
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Can`t process rpc from {topic} by agent {agent}, msg: {result}");
                }

                return null;
            };
        }
    }
}