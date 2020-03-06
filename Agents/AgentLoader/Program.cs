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
        public static void Main(string[] args)
        {
            Console.WriteLine($"PID: {Process.GetCurrentProcess().Id}");
            CreateHostBuilder(args).Build().Run();
        }

        private static IEnumerable<Type> LoadAgents()
        {
            var agentsFolder = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Agents");
            if (Directory.Exists(agentsFolder))
            {
                return Directory.GetFiles(agentsFolder, "*Agent.dll")
                    .SelectMany(x =>
                    {
                        var assembly = new AgentLoadContext(x).LoadFromAssemblyPath(x);
                        return assembly
                            .GetTypes()
                            .Where(type => type.IsSubclassOf(typeof(AgentAbstract)));
                        //  .Select(type => (AgentAbstract) Activator.CreateInstance(type));
                    });
            }

            return new List<Type>();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddKafkaTransport(hostContext.Configuration);

                    var agents = LoadAgents().ToList();
                    // services.TryAddEnumerable(new ServiceDescriptor());
                    services.TryAddEnumerable(agents.Select(agentType =>
                        new ServiceDescriptor(typeof(AgentAbstract),
                            sp =>
                                Activator.CreateInstance(agentType, 
                                    sp.GetService<ITransportSender>()),
                            ServiceLifetime.Singleton)));
                    Console.WriteLine($"Loaded {agents.Count} agent");

                    services.AddHostedService<AgentWorker>();
                });
    }
}