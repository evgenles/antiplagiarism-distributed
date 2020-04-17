using System;
using System.Collections.Generic;
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
            base(transportSender, AgentType.Ui, "", MessageType.Unknown, MessageType.Connection, MessageType.TaskStat)
        {
            _agentUiHub = agentUiHub;
            _taskUiHub = taskUiHub;
            _logger = logger;
        }


        public override async Task ProcessMessageAsync(AgentMessage message, Dictionary<string, string> _)
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

        public override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> UploadDocumentAsync(byte[] document, string taskId) =>
             Transport.SendAsync(AgentType.FileManager.ToString(), document, true,
                new Dictionary<string, string>
                {
                    ["Task"] = taskId
                });

        public ValueTask<bool> CreateNewTask(TaskMessage taskMessage) =>
            Transport.SendAsync(AgentType.Splitter.ToString(), new AgentMessage
            {
                Author = new Author(this),
                MessageType = MessageType.SplitterTask,
                SendDate = DateTime.Now,
                Data = taskMessage
            });
    }
}