using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using Transport.Abstraction;

namespace ConnectionAnalyzerAgent
{
    public class ConnectionAnalyzer : AgentAbstract
    {
        public readonly List<ConnectionState> Connections = new List<ConnectionState>();

        public ConnectionAnalyzer(ITransportSender sender) : base(sender,
            AgentType.ConnectionAnalyzer, "", MessageType.Connection, MessageType.RpcRequest)
        {
            Task.Run(CheckConnectionState);
            Connections.Add(new ConnectionState
            {
                State = AgentState.Online,
                Who = new Author(this),
                LastUpdate = DateTime.Now
            });
        }

        public async Task CheckConnectionState()
        {
            while (!StoppingToken.IsCancellationRequested)
            {
                var toRemove = Connections.Where(x =>
                        x.Who.Id != Id &&
                        x.State == AgentState.Degrading &&
                        x.LastUpdate < DateTime.Now.AddSeconds(-50))
                    .ToList();
                foreach (var disconnected in toRemove)
                {
                    await Transport.SendAsync(MessageType.Connection.ToString(),
                        new AgentMessage
                        {
                            Author = new Author(this),
                            Data = new ConnectionMessage
                            {
                                State = AgentState.Disconnected,
                                Who = disconnected.Who
                            },
                            MessageType = MessageType.Connection,
                            SendDate = DateTime.Now
                        });
                }

                Connections.RemoveAll(x => toRemove.Any(r => r.Who.Id == x.Who.Id));

                var degradedConnections =
                    Connections.Where(x =>
                        x.Who.Id != Id &&
                        x.State != AgentState.Degrading &&
                        x.LastUpdate < DateTime.Now.AddSeconds(-30));
                foreach (var degradedConnection in degradedConnections)
                {
                    degradedConnection.State = AgentState.Degrading;
                    await Transport.SendAsync(MessageType.Connection.ToString(),
                        new AgentMessage
                        {
                            Author = new Author(this),
                            Data = new ConnectionMessage
                            {
                                State = degradedConnection.State,
                                Who = degradedConnection.Who
                            },
                            MessageType = MessageType.Connection,
                            SendDate = DateTime.Now
                        });
                }

                await Task.Delay(30000);
            }
        }

        protected override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
           return message.Data.Type switch
            {
                RpcRequestType.GetAllAgents => Task.FromResult(new AgentMessage
                {
                    Author = new Author(this),
                    MessageType = MessageType.RpcResponse,
                    SendDate = DateTime.Now,
                    Data = Connections.Select(x => new ConnectionMessage {State = x.State, Who = x.Who})
                }),
                _ => Task.FromResult<AgentMessage>(null)
            };
        }

        public override Task ProcessMessageAsync(AgentMessage message)
        {
            var data = message.Data.ToObject<ConnectionMessage>();
            var connectionState = new ConnectionState
            {
                State = data.State,
                Who = data.Who ?? message.Author,
                LastUpdate = DateTime.Now
            };
            var savedConn = Connections.FirstOrDefault(x => x.Who.Id == connectionState.Who.Id);
            if (savedConn != null)
            {
                if (connectionState.State == AgentState.Disconnected)
                {
                    Connections.Remove(savedConn);
                }
                else
                {
                    savedConn.LastUpdate = connectionState.LastUpdate;
                    savedConn.State = connectionState.State;
                }
            }
            else if (connectionState.State != AgentState.Disconnected)
            {
                Connections.Add(connectionState);
            }

            return Task.CompletedTask;
        }
    }
}