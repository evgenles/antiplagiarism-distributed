using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Abstraction;

namespace Transport.KubeMq
{
    public static class StartupExtensions
    {
        
        /// <summary>
        /// Adding kubemq transport.
        /// </summary>
        /// <param name="services">Service collection </param>
        /// <param name="configuration">Configuration</param>
        /// <returns></returns>
        public static IServiceCollection AddKubeMqTransport(this IServiceCollection services)
        {
            services.AddSingleton<ITransportSender, KubeMqTransportSender>();
            services.AddTransient<ITransportConsumer, KubeMqTransportConsumer>();
            return services;
        }
    }
}