using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using AgentLoader.Models;

namespace TestAgent
{
    public class TestAgent2 : AgentAbstract
    {
        public TestAgent2() : base(AgentType.ConnectionAnalyzer, "", MessageType.Connection)
        {
        }

        public override Task<bool> ProcessMessageAsync(AgentMessage message)
        {
            Console.WriteLine($"XXX {message.Author.Type} {message.Author.SubType} connected in {message.SendDate}");
            return Task.FromResult(true);
        }
    }
}