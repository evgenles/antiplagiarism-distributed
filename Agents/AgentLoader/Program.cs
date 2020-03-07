using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Agent.Abstract;
using AgentLoader.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transport.Abstraction;
using Transport.Kafka;

namespace AgentLoader
{
    public class Program
    {
        private static readonly List<AgentAssemblyLoaderContext> AgentContexts = new List<AgentAssemblyLoaderContext>();

        public static void Main(string[] args)
        {
            Console.WriteLine($"PID: {Process.GetCurrentProcess().Id}");
            var agentsFolder = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Agents");
            if (Directory.Exists(agentsFolder))
            {
                foreach (var directory in Directory.GetDirectories(agentsFolder))
                {
                    AgentContexts.Add(new AgentAssemblyLoaderContext(directory,
                        typeof(AgentAbstract)));
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
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddKafkaTransport(hostContext.Configuration);

                    var agents = AgentAssemblyLoaderContext.AgentTypes;
                    agents.ForEach(type => services.AddTransient(type));
                    services.AddSingleton<IAgentProvider, AgentProvider>();
                    // services.TryAddEnumerable(agents.Select(agentType =>
                    //     ServiceDescriptor.Singleton<AgentAbstract>(
                    //         sp =>
                    //             Activator.CreateInstance(agentType, 
                    //                 sp.GetService<ITransportSender>()))));
                    Console.WriteLine($"Loaded {agents.Count} agent in {AgentContexts.Count} context");

                    services.AddHostedService<AgentWorker>();
                });
    }
}