using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;

namespace ServiceBusDelayedProcessing;

public class DelayedProcessingFunction
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly SampleDbContext _dbContext;

    public DelayedProcessingFunction(ServiceBusClient serviceBusClient, SampleDbContext dbContext)
    {
        _serviceBusClient = serviceBusClient;
        _dbContext = dbContext;
    }

    [FunctionName(nameof(DelayedProcessingFunction))]
    public async Task RunAsync([ServiceBusTrigger("order")] CheckOrderPaymentStatus message, ILogger log)
    {
        log.LogInformation("C# ServiceBus queue trigger function processed message: @{message}", message);
        var order = GetOrderById(message.Id);
        var success = ProcessPayment(order);
        if (!success)
        {
            await CheckPaymentStatusLaterAsync(order.Id, message.RetryAttempt++);
            return;
        }

        order.Complete();
        _dbContext.SaveChanges();
    }

    private static Order GetOrderById(int orderId)
    {
        return new Faker<Order>().RuleFor(o => o.Id, orderId);
    }

    private static bool ProcessPayment(Order order)
    {
        try
        {
            // re-trieve customer infor related to order
            // and call related payment gateway provider
            return false;
        }
        catch (PaymentGatewayException)
        {
            return false;
        }
    }

    private async Task<bool> CheckPaymentStatusLaterAsync(int id, int retryAttempt)
    {
        if (retryAttempt > 3) return false;
        var delayedTimeSpan = retryAttempt switch
        {
            1 => TimeSpan.FromMinutes(30),
            2 => TimeSpan.FromHours(3),
            _ => TimeSpan.FromHours(12)
        };
        await using var sender = _serviceBusClient.CreateSender("order");
        var message = new ServiceBusMessage(JsonConvert.SerializeObject(new CheckOrderPaymentStatus { Id = id, RetryAttempt = retryAttempt }));
        var seq = await sender.ScheduleMessageAsync(message, DateTimeOffset.Now.Add(delayedTimeSpan));
        return true;
    }
}
