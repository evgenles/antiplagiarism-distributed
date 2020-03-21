﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ui.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
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