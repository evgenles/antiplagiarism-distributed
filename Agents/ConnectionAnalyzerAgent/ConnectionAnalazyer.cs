using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;

namespace ConnectionAnalyzerAgent
{
    public class ConnectionAnalyzer : AgentAbstract
    {
        public List<ConnectionState> Connections = new List<ConnectionState>();

        public ConnectionAnalyzer() : base(AgentType.ConnectionAnalyzer, "", MessageType.Connection)
        {
            Task.Run(CheckConnectionState);
        }

        public async Task CheckConnectionState()
        {
            while (!StoppingToken.IsCancellationRequested)
            {
                var toRemove = Connections.Where(x => x.State == AgentState.Degrading &&
                                                      x.LastUpdate < DateTime.Now.AddSeconds(-50))
                    .ToList();
                foreach (var disconnected in toRemove)
                {
                    await SendMessageAsync(new AgentMessage
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
                    Connections.Where(x => x.State != AgentState.Degrading &&  x.LastUpdate < DateTime.Now.AddSeconds(-30));
                foreach (var degradedConnection in degradedConnections)
                {
                    degradedConnection.State = AgentState.Degrading;
                    await SendMessageAsync(new AgentMessage
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

        public override Task<bool> ProcessMessageAsync(AgentMessage message)
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

            return Task.FromResult(true);
        }
    }
}