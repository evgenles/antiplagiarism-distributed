using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using AgentLoader;
using AgentLoader.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Transport.Abstraction;
using Ui.Server.Hubs;
using Ui.Shared;

namespace Ui.Server
{
    public class UiAgent : AgentAbstract
    {
        private readonly IHubContext<AgentHub> _agentUiHub;
        private readonly IHubContext<TaskHub> _taskUiHub;
        private readonly ILogger<UiAgent> _logger;

        public UiAgent(IHubContext<AgentHub> agentUiHub, IHubContext<TaskHub> taskUiHub, ILogger<UiAgent> logger,
            ITransportSender transportSender) :
            base(transportSender, AgentType.Ui, "", MessageType.Connection, MessageType.TaskStat)
        {
            _agentUiHub = agentUiHub;
            _taskUiHub = taskUiHub;
            _logger = logger;
        }

        public async Task<TResp> CallAsync<TReq, TResp>(TReq msg, TimeSpan timeout)
        {
            return await Transport.CallServiceAsync<TReq, TResp>(MessageType.RpcRequest.ToString(), msg, timeout);
        }

        public override async Task ProcessMessageAsync(AgentMessage message)
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
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t process message");
            }
        }

        protected override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            throw new NotImplementedException();
        }
    }
}