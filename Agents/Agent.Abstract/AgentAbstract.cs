using System;
using System.Collections.Generic;
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
        public virtual AgentType Type { get; }

        public virtual MessageType RpcMessageType { get; }

        public virtual MessageType[] SupportedMessage { get; }

        public virtual string SubType { get; }

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

        protected AgentAbstract(ITransportSender transport, AgentType type, string subType = "",
            MessageType rpcType = MessageType.Unknown, params MessageType[] supportedMessage)
        {
            Transport = transport;
            Type = type;
            RpcMessageType = rpcType;
            SubType = subType;
            MachineName = Environment.MachineName;
            Version = GetType().Assembly.GetName().Version.ToString();
            Ip = string.Join(", ", Dns.GetHostAddresses(Dns.GetHostName())
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .Select(x => x.ToString()));
            SupportedMessage = supportedMessage;
            Id = Guid.NewGuid();
            State = AgentState.Online;
            Task.Run(SendHearthBeat);
        }

        public abstract Task ProcessMessageAsync(AgentMessage message);

        public virtual Task ProcessMessageAsync(byte[] clearByteMessage, Dictionary<string, string> headers)
        {
            return Task.CompletedTask;
        }

        // public async Task ProcessRpcAsync(AgentMessage<RpcRequest> message, string responseTo)
        // {
        //     var result = await ProcessRpcAsync(message);
        //     if (result != null)
        //         await Transport.SendAsync(responseTo, result);
        // }

        public abstract Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message);
        public virtual async Task<TResp> CallAsync<TResp>(AgentMessage msg, TimeSpan timeout)
        {
            return await Transport.CallServiceAsync<AgentMessage, TResp>(msg.MessageType.ToString(), msg, timeout);
        }
        
        public virtual async Task<AgentMessage<TResp>> CallAsync<TResp>(AgentMessage<RpcRequest> msg, TimeSpan timeout) where TResp : class
        {
            var resp =  await Transport.CallServiceAsync<AgentMessage<RpcRequest>, AgentMessage>(MessageType.ConnectionRequest.ToString(), msg, timeout);
            return resp?.To<TResp>();
        }


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
            await Transport.SendAsync(MessageType.Connection.ToString(), new AgentMessage
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