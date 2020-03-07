using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Agent.Abstract.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Ui.Server.Hubs
{
    public class AgentHub : Hub
    {
        private readonly UiAgent _uiAgent;
        private readonly ILogger<AgentHub> _logger;

        public AgentHub(UiAgent uiAgent, ILogger<AgentHub> logger)
        {
            _uiAgent = uiAgent;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var msg = new AgentMessage<RpcRequest>
            {
                Author = new Author(_uiAgent),
                Data = new RpcRequest
                {
                    Type = RpcRequestType.GetAllAgents,
                    RequestedAgent = AgentType.ConnectionAnalyzer
                },
                SendDate = DateTime.Now
            };
            var result = await _uiAgent.CallAsync<List<ConnectionMessage>>(msg, TimeSpan.FromSeconds(30));
            var recived = DateTime.Now;
            if (result != null)
            {
                _logger.LogInformation(JsonSerializer.Serialize(result.Data));
                // var response = result.To<List<ConnectionMessage>>();
                await Clients.Caller.SendCoreAsync("ConnectAccepted", new object[] {result});
            }

            await base.OnConnectedAsync();
        }
    }
}