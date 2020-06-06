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
            string agentId = $"{agent.Type}_{agent.SubType}";
            _consumer.Subscribe(agentId, agent.RpcMessageType.ToString(), stoppingToken,
                agentSupported
            );
            _consumer.OnConsumed += async (id, result, topic, forceBytes, headers) =>
            {
                if (agentId == id && agentSupported.Contains(topic))
                {
                    if (!forceBytes)
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
                    else
                    {
                        await agent.ProcessMessageAsync(result, headers);
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