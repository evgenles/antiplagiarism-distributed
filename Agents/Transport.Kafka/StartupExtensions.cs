using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Abstraction;

namespace Transport.Kafka
{
    public static class StartupExtensions
    {
        
        /// <summary>
        /// Adding kafka transport. Please specify kafka configuration of type <see cref="KafkaConfiguration"/> in configuration
        /// </summary>
        /// <param name="services">Service collection </param>
        /// <param name="configuration">Configuration</param>
        /// <returns></returns>
        public static IServiceCollection AddKafkaTransport(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KafkaConfiguration>(configuration.GetSection("Kafka"));
            services.AddSingleton<ITransportSender, KafkaTransportSender>();
            services.AddTransient<ITransportConsumer, KafkaTransportConsumer>();
            return services;
        }
    }
}