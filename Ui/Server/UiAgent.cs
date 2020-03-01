using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using AgentLoader;
using AgentLoader.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Ui.Server.Hubs;
using Ui.Shared;

namespace Ui.Server
{
    public class UiAgent : AgentAbstract
    {
        private readonly IHubContext<AgentHub> _agentUiHub;
        private readonly IHubContext<TaskHub> _taskUiHub;
        private readonly ILogger<UiAgent> _logger;

        public UiAgent(IHubContext<AgentHub> agentUiHub, IHubContext<TaskHub> taskUiHub, ILogger<UiAgent> logger) :
            base(AgentType.Ui, "", MessageType.Connection, MessageType.TaskStat)
        {
            _agentUiHub = agentUiHub;
            _taskUiHub = taskUiHub;
            _logger = logger;
        }

        public override async Task<bool> ProcessMessageAsync(AgentMessage message)
        {
            try
            {
                switch (message.MessageType)
                {
                    case MessageType.Connection:
                        var connectionMessage = message.Data.ToObject<ConnectionMessage>();
                        connectionMessage.Who ??= message.Author;
                        await _agentUiHub.Clients.All.SendAsync(SignalRMessages.AgentConnections.ToString(),
                            connectionMessage);
                        return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t process message");
            }

            return false;
        }
    }
}