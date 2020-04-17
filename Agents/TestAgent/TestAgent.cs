using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using Transport.Abstraction;

namespace TestAgent
{
    public class TestAgent : AgentAbstract
    {
        public TestAgent(ITransportSender transportSender) : base(transportSender, 
            AgentType.Splitter, "", MessageType.TaskRequest, MessageType.SplitterTask)
        {
        }

        public override Task ProcessMessageAsync(AgentMessage message, Dictionary<string, string> _)
        {
            throw new NotImplementedException();
        }

        public override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            throw new NotImplementedException();
        }
    }
}