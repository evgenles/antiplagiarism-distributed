using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Abstract.Models;

namespace Agent.Abstract
{
    public interface IAgent
    {
        AgentType Type { get; }

        string SubType { get; }

        Task ProcessMessageAsync(AgentMessage message);
        Task ProcessMessageAsync(byte[] clearByteMessage, Dictionary<string, string> headers);
        Task<AgentMessage<TResp>> CallAsync<TResp>(AgentMessage<RpcRequest> msg, TimeSpan timeout) where TResp : class;

        Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message);
    }
}