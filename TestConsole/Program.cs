using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using AdvegoPlagiatusWorker;
using Agent.Abstract.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder().ConfigureLogging(x => x.AddConsole()).Build();
            var agent = new AdvegoAgent(null, host.Services.GetService<ILogger<AdvegoAgent>>());
            await agent.ProcessMessageAsync(new AgentMessage<TaskMessage>()
            {
                Data = new TaskMessage
                {
                    Id = Guid.NewGuid()
                }
            });
        }
    }
}