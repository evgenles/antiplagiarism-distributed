using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;

namespace TestAgent
{
    public class TestAgent : AgentAbstract
    {
        public TestAgent() : base(AgentType.Splitter, "", MessageType.Task)
        {
        }

        public override Task<AgentMessage> ProcessMessageAsync(AgentMessage message)
        {
            throw new NotImplementedException();
        }
    }
}