using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(ServiceBusDelayedProcessing.Startup))]
namespace ServiceBusDelayedProcessing;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;
        var services = builder.Services;

        services
            .AddSingleton(new ServiceBusClient(configuration["AzureWebJobsServiceBus"]))
            .AddSingleton<SampleDbContext>();
    }
}