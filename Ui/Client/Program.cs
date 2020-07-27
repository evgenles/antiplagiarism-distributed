using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Ui.Shared;

namespace Ui.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<PageShared>();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("uk-UA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("uk-UA");
            CultureInfo.DefaultThreadCurrentCulture  = CultureInfo.GetCultureInfo("uk-UA");
            CultureInfo.DefaultThreadCurrentCulture  = CultureInfo.GetCultureInfo("uk-UA");
            builder.RootComponents.Add<App>("app");
            var host = builder.Build();
            await host.RunAsync();
        }
    }
}