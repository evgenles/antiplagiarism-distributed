using System.Threading.Tasks;
using AgentLoader.Models;

namespace AgentLoader.Agent
{
    public interface IAgent
    {
        public AgentType Type { get; }
        
        public string SubType { get; }

        public Task<bool> ProcessMessage(AgentMessage message);
        
    }
}