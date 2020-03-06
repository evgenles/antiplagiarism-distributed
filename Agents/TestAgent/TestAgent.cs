using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using Transport.Abstraction;

namespace TestAgent
{
    public class TestAgent : AgentAbstract
    {
        public TestAgent(ITransportSender transportSender) : base(transportSender, 
            AgentType.Splitter, "", MessageType.Task)
        {
        }

        public override Task ProcessMessageAsync(AgentMessage message)
        {
            throw new NotImplementedException();
        }

        protected override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            throw new NotImplementedException();
        }
    }
}