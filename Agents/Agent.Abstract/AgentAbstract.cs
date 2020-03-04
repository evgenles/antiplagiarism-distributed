using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Agent.Abstract.Models;
using Transport.Abstraction;

namespace Agent.Abstract
{
    public abstract class AgentAbstract : IAgent, IDisposable
    {
        protected ITransportSender Transport { get; }
        public virtual AgentType Type { get;}
        
        public virtual MessageType[] SupportedMessage { get; }
        
        public virtual string SubType { get;}
        
        public string Version { get; }
        
        public string Ip { get; }
        
        public string MachineName { get; }

        public Guid Id { get; }
        
        public AgentState State { get; set; }
        
        // public Func<AgentMessage, Task> SendMessageAsync { get; set; }
        //
        // /// <summary>
        // /// Make async call to another agent. Call with (request message, timeout)
        // /// </summary>
        // public Func<AgentMessage<RpcRequest>, TimeSpan, Task<AgentMessage>> CallAsync { get; set; }
        //

        public CancellationToken StoppingToken { get; set; } = CancellationToken.None;
        
        protected AgentAbstract(ITransportSender transport, AgentType type, string subType = "", params  MessageType[] supportedMessage)
        {
            Transport = transport;
            Type = type;
            SubType = subType;
            MachineName = Environment.MachineName;
            Version = GetType().Assembly.GetName().Version.ToString();
            Ip = string.Join(", " ,Dns.GetHostAddresses(Dns.GetHostName())
                .Where(x=>x.AddressFamily == AddressFamily.InterNetwork)
                .Select(x=>x.ToString()));
            SupportedMessage = supportedMessage;
            Id = Guid.NewGuid();
            State = AgentState.Online;
            Task.Run(SendHearthBeat);
        }
        public abstract Task<AgentMessage> ProcessMessageAsync(AgentMessage message);

        private async Task SendConnectAsync()
        {
            await Transport.SendAsync(MessageType.Connection.ToString(), new AgentMessage
            {
                Author = this,
                Data = new ConnectionMessage
                {
                    State = AgentState.Connected
                },
                MessageType = MessageType.Connection,
                SendDate = DateTime.Now
            });
        }
        
        private async Task SendHearthBeat()
        {
            await SendConnectAsync();
            while (!StoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, StoppingToken);
                await Transport.SendAsync(MessageType.Connection.ToString(), new AgentMessage
                {
                    Author = this,
                    Data = new ConnectionMessage
                    {
                        State = State
                    },
                    MessageType = MessageType.Connection,
                    SendDate = DateTime.Now
                });
            }
        }
        private async Task SendDisconnectAsync()
        {
            await Transport.SendAsync(MessageType.Connection.ToString(),new AgentMessage
            {
                Author = this,
                Data = new ConnectionMessage
                {
                    State = AgentState.Disconnected
                },
                MessageType = MessageType.Connection,
                SendDate = DateTime.Now
            });
        }

        public void Dispose()
        {
            SendDisconnectAsync().GetAwaiter().GetResult();
        }
    }
}