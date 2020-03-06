﻿using System.Threading.Tasks;
using Agent.Abstract.Models;

namespace Agent.Abstract
{
    public interface IAgent
    {
        public AgentType Type { get; }
        
        public string SubType { get; }

        public Task ProcessMessageAsync(AgentMessage message);

        public Task ProcessRpcAsync(AgentMessage<RpcRequest> message, string responseTo);

    }
}