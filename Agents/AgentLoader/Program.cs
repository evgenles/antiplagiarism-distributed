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

namespace AgentLoader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"PID: {Process.GetCurrentProcess().Id}");
            CreateHostBuilder(args).Build().Run();
        }

        private static IEnumerable<AgentAbstract> LoadAgents()
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
                            .Where(type => type.IsSubclassOf(typeof(AgentAbstract)))
                            .Select(type => (AgentAbstract) Activator.CreateInstance(type));
                    });

            }
            return new List<AgentAbstract>();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<Configuration>(hostContext.Configuration.GetSection("Conf"));
                    var agents = LoadAgents().ToList();
                    Console.WriteLine($"Loaded {agents.Count} agent");

                        services.AddHostedService((c)=>new AgentWorker(agents,
                            c.GetService<IOptions<Configuration>>(), c.GetService<ILogger<AgentWorker>>()));
                });
    }
}