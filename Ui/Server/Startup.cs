using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using Agent.Abstract;
using AgentLoader;
using AgentLoader.Models;
using FileWorkerAgent;
using Microsoft.Extensions.Configuration;
using Transport.Kafka;
using Transport.KubeMq;
using Ui.Server.Hubs;

namespace Ui.Server
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddKubeMqTransport();
            //services.AddKafkaTransport(_configuration);
            services.AddSingleton<UiAgent>();
            services.AddSingleton<IFileWorkerAgent, FileWorkerAgentImpl>();
            services.AddSingleton<IAgentProvider>((sp)=> new AgentProvider(sp, typeof(UiAgent)));
            services.AddHostedService<AgentWorker>();

            services.AddSignalR()
                .AddHubOptions<TaskHub>(opt=>
                {
                    opt.MaximumReceiveMessageSize = long.MaxValue;
                    opt.EnableDetailedErrors = true;
                });
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }

            app.UseStaticFiles();
            app.UseBlazorFrameworkFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHub<AgentHub>("/agentHub");
                endpoints.MapHub<TaskHub>("/taskHub");
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
