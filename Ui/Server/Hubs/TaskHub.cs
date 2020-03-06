using System;
using System.Threading.Tasks;
using Agent.Abstract.Models;
using AgentLoader.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Ui.Server.Hubs
{
    public class TaskHub : Hub
    {
        private readonly UiAgent _agent;
        private readonly ILogger<TaskHub> _logger;

        public TaskHub(UiAgent agent, ILogger<TaskHub> logger)
        {
            _agent = agent;
            _logger = logger;
        }
        
        public async Task AddTask(TaskMessage message)
        {
            // await _agent.SendMessageAsync(new AgentMessage
            // {
            //     Author = new Author(_agent),
            //     MessageType = MessageType.Task,
            //     SendDate = DateTime.Now,
            //     Data = message
            // });
            await Clients.All.SendAsync("TaskAdded", message);
        }
    }
}