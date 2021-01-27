using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using OpenSleigh.Core.BackgroundServices;
using OpenSleigh.Core.Messaging;
using OpenSleigh.Core.Utils;

namespace OpenSleigh.Core.DependencyInjection
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenSleigh(this IServiceCollection services, Action<IBusConfigurator> configure = null)
        {
            var typeResolver = new TypeResolver();
            var sagaTypeResolver = new SagaTypeResolver(typeResolver);

            services.AddSingleton<ISagaTypeResolver>(sagaTypeResolver)
                .AddSingleton<ISagasRunner, SagasRunner>()
                .AddSingleton<ITypesCache, TypesCache>()
                .AddSingleton<ITypeResolver>(typeResolver)
                .AddSingleton<ISerializer, JsonSerializer>()
                .AddSingleton<IMessageContextFactory, DefaultMessageContextFactory>()
                .AddScoped<IMessageBus, DefaultMessageBus>()
                .AddScoped<IMessageProcessor, MessageProcessor>()
                .AddHostedService<SubscribersBackgroundService>()

                .AddScoped<IOutboxProcessor, OutboxProcessor>()
                .AddSingleton(OutboxProcessorOptions.Default)
                .AddHostedService<OutboxBackgroundService>()
                
                .AddScoped<IOutboxCleaner, OutboxCleaner>()
                .AddSingleton(OutboxCleanerOptions.Default)
                .AddHostedService<OutboxCleanerBackgroundService>();

            var builder = new BusConfigurator(services, sagaTypeResolver);
            configure?.Invoke(builder);
            
            return services;
        }
    }

}