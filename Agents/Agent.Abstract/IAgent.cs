using System.Threading.Tasks;
using Agent.Abstract.Models;

namespace Agent.Abstract
{
    public interface IAgent
    {
        public AgentType Type { get; }
        
        public string SubType { get; }

        public Task<bool> ProcessMessageAsync(AgentMessage message);
        
    }
}