using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using AgentLoader;
using AgentLoader.Models;
using FileWorkerAgent;
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
        private readonly IFileWorkerAgent _fileWorkerAgent;

        public UiAgent(IHubContext<AgentHub> agentUiHub, IHubContext<TaskHub> taskUiHub, ILogger<UiAgent> logger,
            ITransportSender transportSender, IFileWorkerAgent fileWorkerAgent) :
            base(transportSender, AgentType.Ui, "", MessageType.Unknown, MessageType.Connection, MessageType.TaskStat)
        {
            _agentUiHub = agentUiHub;
            _taskUiHub = taskUiHub;
            _logger = logger;
            _fileWorkerAgent = fileWorkerAgent;
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

        public override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<bool> UploadDocumentAsync(byte[] document, string taskId)
        {
            try
            {
                await _fileWorkerAgent.UploadFileAsync(taskId, document);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when loading file for task {@taskId}", taskId);
                return false;
            }
        }
        public ValueTask<bool> CreateNewTask(TaskMessage taskMessage) =>
            Transport.SendAsync(MessageType.SplitterTask.ToString(), new AgentMessage
            {
                Author = new Author(this),
                MessageType = MessageType.SplitterTask,
                SendDate = DateTime.Now,
                Data = taskMessage
            });
    }
}