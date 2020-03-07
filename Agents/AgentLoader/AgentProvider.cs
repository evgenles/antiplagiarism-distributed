using System;
using System.Collections.Generic;
using System.Linq;
using Agent.Abstract;

namespace AgentLoader
{
    public class AgentProvider : IAgentProvider
    {
        private IServiceProvider _serviceProvider;
        public List<AgentAbstract> Agents { get; }

        public AgentProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Agents = AgentAssemblyLoaderContext.AgentTypes.Select(agentType =>
                    (AgentAbstract) serviceProvider.GetService(agentType))
                .ToList();
        }

        public AgentProvider(IServiceProvider serviceProvider, params Type[] types)
        {
            Agents = types.Select(agentType =>
                    (AgentAbstract) serviceProvider.GetService(agentType))
                .ToList();
        }

    }
}