using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using AgentLoader.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentLoader
{
    public class AgentWorker : BackgroundService
    {
        private readonly List<AgentAbstract> _agents;
        private readonly ILogger<AgentWorker> _logger;
        private readonly Configuration _configuration;

        public AgentWorker(List<AgentAbstract> agents, IOptions<Configuration> configuration,
            ILogger<AgentWorker> logger)
        {
            _agents = agents;
            _logger = logger;
            _configuration = configuration.Value;
            foreach (var agent in agents)
            {
                agent.SendMessageAsync = SendMessageAsync;
            }
        }

        private async Task SendMessageAsync(AgentMessage message)
        {
            var config = new ProducerConfig {BootstrapServers = _configuration.KafkaBootstrap};

            Action<DeliveryReport<Null, string>> handler = r =>
                Console.WriteLine(!r.Error.IsError
                    ? $"Delivered message to {r.TopicPartitionOffset}"
                    : $"Delivery Error: {r.Error.Reason}");

            using var producer = new ProducerBuilder<string, string>(config).Build();
            await producer.ProduceAsync(message.MessageType.ToString(), new Message<string, string>()
            {
                Key = message.Author.Type.ToString(),
                Value = JsonSerializer.Serialize(message)
            });

            producer.Flush(TimeSpan.FromSeconds(10));
        }

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
                    await SendMessageAsync(new AgentMessage
                    {
                        Author = new Author(agent),
                        Data = new ConnectionMessage
                        {
                            State = AgentState.Disconnected
                        },
                        MessageType = MessageType.Connection,
                        SendDate = DateTime.Now
                    });
                }
                await base.StopAsync(cancellationToken);
            }
            catch (Exception e) when(e !is OperationCanceledException)
            {
                Console.WriteLine(e);
            }

        }

        private async Task SendHeartbeatAsync(AgentAbstract agent, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                await SendMessageAsync(new AgentMessage
                {
                    Author = new Author(agent),
                    Data = new ConnectionMessage
                    {
                        State = agent.State
                    },
                    MessageType = MessageType.Connection,
                    SendDate = DateTime.Now
                });
            }
        }

        private async Task AgentExecutable(AgentAbstract agent, CancellationToken stoppingToken)
        {
            agent.StoppingToken = stoppingToken;
            var config = new ConsumerConfig()
            {
                BootstrapServers = _configuration.KafkaBootstrap,
                Acks = Acks.All,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                GroupId = agent.Type + "_" + agent.SubType
            };
            var heartbeatTask = Task.Run(() => SendHeartbeatAsync(agent, stoppingToken), stoppingToken);
            var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(agent.SupportedMessage.Select(x => x.ToString()));

            await SendMessageAsync(new AgentMessage
            {
                Author = new Author(agent),
                Data = new ConnectionMessage
                {
                    State = AgentState.Connected
                },
                MessageType = MessageType.Connection,
                SendDate = DateTime.Now
            });
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume();
                    var msg = JsonSerializer.Deserialize<AgentMessage>(result.Message.Value);
                    if (msg.Author.Id != agent.Id)
                    {
                        agent.State = AgentState.InWork;
                        _logger.LogInformation($"Consumed message {result.Key}, {result.Value}");
                        await agent.ProcessMessageAsync(msg);
                    }

                    consumer.Commit();
                }
                catch (Exception e) when (e ! is OperationCanceledException)
                {
                    Console.WriteLine(e);
                }

                agent.State = AgentState.Online;
            }

            await heartbeatTask;
        }
    }
}