using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using AgentLoader.Models;

namespace TestAgent
{
    public class TestAgent : AgentAbstract
    {
        public TestAgent() : base(AgentType.Ui, "", MessageType.TaskStat)
        {
        }

        public override Task<bool> ProcessMessageAsync(AgentMessage message)
        {
            throw new NotImplementedException();
        }
    }
}