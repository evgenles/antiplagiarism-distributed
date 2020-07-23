using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Agent.Abstract;
using AgentLoader.Models;
using FileWorkerAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transport.Abstraction;
using Transport.Kafka;
using Transport.KubeMq;

namespace AgentLoader
{
    public class Program
    {
        private static readonly List<AgentAssemblyLoaderContext> AgentContexts = new List<AgentAssemblyLoaderContext>();

        public static void Main(string[] args)
        {
            Console.WriteLine($"PID: {Process.GetCurrentProcess().Id}");
            var agentsFolder = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "Agents");
            if (Directory.Exists(agentsFolder))
            {
                foreach (var directory in Directory.GetDirectories(agentsFolder))
                {
                    AgentContexts.Add(new AgentAssemblyLoaderContext(directory,
                        typeof(AgentAbstract), typeof(ITransportConsumer),
                        typeof(ITransportSender), typeof(ILogger), typeof(IFileWorkerAgent)));
                }
                if(AgentContexts.Count == 0)
                    throw new ArgumentException("Can`t load any agent context in `Agents\\*` directory");
            }
            else
            {
                throw new ArgumentException("Can`t find folder `Agents`");
            }

            CreateHostBuilder(args).Build().Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(log=>log.AddConsole())
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddKubeMqTransport();
                    //services.AddKafkaTransport(hostContext.Configuration);
                    services.AddSingleton<IFileWorkerAgent, FileWorkerAgentImpl>();
                    var agents = AgentAssemblyLoaderContext.AgentTypes;
                    agents.ForEach(type => services.AddTransient(type));
                    services.AddSingleton<IAgentProvider, AgentProvider>();
                    Console.WriteLine($"Loaded {agents.Count} agent in {AgentContexts.Count} context");

                    services.AddHostedService<AgentWorker>();
                });
    }
}