using System.Threading.Tasks;
using AgentLoader.Models;

namespace AgentLoader.Agent
{
    public abstract class AgentAbstract : IAgent
    {
        public abstract AgentType Type { get; }
        public abstract string SubType { get; }
        public abstract Task<bool> ProcessMessage(AgentMessage message);

        public void Replay()
        {
            
        }
    }
}