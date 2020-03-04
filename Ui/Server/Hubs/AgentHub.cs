using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Abstract.Models;
using Microsoft.AspNetCore.SignalR;

namespace Ui.Server.Hubs
{
    public class AgentHub : Hub
    {
        private readonly UiAgent _uiAgent;

        public AgentHub(UiAgent uiAgent)
        {
            _uiAgent = uiAgent;
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
            var result = await _uiAgent.CallAsync(msg, TimeSpan.FromSeconds(30));
            var recived = DateTime.Now;
            if (result != null)
            {
                var response = result.To<List<ConnectionMessage>>();
                await Clients.Caller.SendCoreAsync("ConnectAccepted", new object[] {response});
            }

            await base.OnConnectedAsync();
        }
    }
}