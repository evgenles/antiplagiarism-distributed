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
using Microsoft.Extensions.Configuration;
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
            services.Configure<Configuration>(_configuration.GetSection("Conf"));
            
            services.AddSingleton<UiAgent>();
            services.AddSingleton((c) => new List<AgentAbstract> {c.GetService<UiAgent>()});
            services.AddHostedService<AgentWorker>();

            services.AddSignalR();
            
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBlazorDebugging();
            }

            app.UseStaticFiles();
            app.UseClientSideBlazorFiles<Client.Program>();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHub<AgentHub>("/agentHub");
                endpoints.MapHub<TaskHub>("/taskHub");
                endpoints.MapFallbackToClientSideBlazor<Client.Program>("index.html");
            });
        }
    }
}
