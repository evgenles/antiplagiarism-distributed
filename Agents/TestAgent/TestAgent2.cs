using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using Transport.Abstraction;

namespace TestAgent
{
    public class TestAgent2 : AgentAbstract
    {
        public TestAgent2(ITransportSender transportSender) : base(transportSender, AgentType.Worker, "", MessageType.Task)
        {
        }

        public override Task ProcessMessageAsync(AgentMessage message)
        {
            Console.WriteLine($"XXX {message.Author.Type} {message.Author.SubType} connected in {message.SendDate}");
            return Task.FromResult<AgentMessage>(null);
        }

        protected override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            throw new NotImplementedException();
        }
    }
}