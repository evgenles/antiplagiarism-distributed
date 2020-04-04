using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Agent.Abstract.Models;
using AgentLoader.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Ui.Shared;

namespace Ui.Server.Hubs
{
    public class TaskHub : Hub
    {
        private readonly UiAgent _uiAgent;
        private readonly ILogger<TaskHub> _logger;

        public TaskHub(UiAgent agent, ILogger<TaskHub> logger)
        {
            _uiAgent = agent;
            _logger = logger;
        }

        public async Task CreateTask(TaskMessage message)
        {
            if (await _uiAgent.CreateNewTask(message))
            {
                message.Data = new byte[0];
                await Clients.All.SendAsync(SignalRMessages.TaskStateChanged.ToString(), message);
            }
        }

        public override async Task OnConnectedAsync()
        {
            // var msg = new AgentMessage<RpcRequest>
            // {
            //     Author = new Author(_uiAgent),
            //     Data = new RpcRequest
            //     {
            //         Type = RpcRequestType.GetAllTasks,
            //         RequestedAgent = AgentType.DbManager
            //     },
            //     MessageType = MessageType.DbRequest,
            //     SendDate = DateTime.Now
            // };
            // var result =
            //     await _uiAgent.CallAsync<Dictionary<Guid, TaskWithSubTasks>>(msg, TimeSpan.FromSeconds(30));
            // if (result != null)
            // {
            //     _logger.LogInformation(JsonSerializer.Serialize(result.Data));
            //     await Clients.Caller.SendCoreAsync(SignalRMessages.TasksConnectAccepted.ToString(),
            //         new object[] {result.Data});
            // }

            await base.OnConnectedAsync();
        }
    }
}