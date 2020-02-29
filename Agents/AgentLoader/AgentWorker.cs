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
            Task.WaitAll(tasks);
            return Task.CompletedTask;
        }

        private async Task AgentExecutable(AgentAbstract agent, CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig()
            {
                BootstrapServers = _configuration.KafkaBootstrap,
                Acks = Acks.All,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                GroupId = agent.Type + "_" + agent.SubType
            };
            var consumer = new ConsumerBuilder<string, string>(config).Build();
            foreach (var support in agent.SupportedMessage)
            {
                consumer.Subscribe(support.ToString());
            }

            await SendMessageAsync(new AgentMessage
            {
                Author = new Author(agent),
                Data = "Connected",
                MessageType = MessageType.Connection,
                SendDate = DateTime.Now
            });
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume();
                    _logger.LogInformation($"Consumed message {result.Key}, {result.Value}");
                    await agent.ProcessMessageAsync(JsonSerializer.Deserialize<AgentMessage>(result.Message.Value));
                    consumer.Commit();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}