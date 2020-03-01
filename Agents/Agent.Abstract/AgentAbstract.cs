using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Agent.Abstract.Models;

namespace Agent.Abstract
{
    public abstract class AgentAbstract : IAgent
    {
        public virtual AgentType Type { get;}
        
        public virtual MessageType[] SupportedMessage { get; }
        
        public virtual string SubType { get;}
        
        public string Version { get; }
        
        public string Ip { get; }
        
        public string MachineName { get; }

        public Guid Id { get; }
        
        public AgentState State { get; set; }
        
        public Func<AgentMessage, Task> SendMessageAsync { get; set; }
        
        public CancellationToken StoppingToken { get; set; } = CancellationToken.None;
        
        protected AgentAbstract(AgentType type, string subType = "", params  MessageType[] supportedMessage)
        {
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
        }
        public abstract Task<bool> ProcessMessageAsync(AgentMessage message);
      
    }
}