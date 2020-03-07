using System.Collections;
using System.Collections.Generic;
using Agent.Abstract;

namespace AgentLoader
{
    public interface IAgentProvider
    {
        List<AgentAbstract> Agents { get; }
    }
}