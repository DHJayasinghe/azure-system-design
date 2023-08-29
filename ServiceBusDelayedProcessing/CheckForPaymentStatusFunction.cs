using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;

namespace ServiceBusDelayedProcessing;

public class CheckForPaymentStatusFunction
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly SampleDbContext _dbContext;
    private readonly static Random random = new();

    public CheckForPaymentStatusFunction(ServiceBusClient serviceBusClient, SampleDbContext dbContext)
    {
        _serviceBusClient = serviceBusClient;
        _dbContext = dbContext;
    }

    [FunctionName(nameof(CheckForPaymentStatusFunction))]
    public async Task RunAsync([ServiceBusTrigger("order")] CheckOrderPaymentStatus message, ILogger log)
    {
        log.LogInformation("C# ServiceBus queue trigger function processed message: @{message}", message);
        var order = GetOrderById(message.Id);
        var paymentStatus = CheckPaymentStatus(order);
        if (paymentStatus == "processing")
        {
            await CheckPaymentStatusLaterAsync(order.Id, message.RetryAttempt++);
            return;
        }

        order.Complete();
        _dbContext.SaveChanges();
    }

    private async Task<bool> CheckPaymentStatusLaterAsync(int id, int retryAttempt)
    {
        if (retryAttempt > 3) return false;
        var delayedTimeSpan = retryAttempt switch
        {
            1 => TimeSpan.FromHours(1),
            2 => TimeSpan.FromHours(6),
            _ => TimeSpan.FromHours(24)
        };
        await using var sender = _serviceBusClient.CreateSender("order");
        var message = new ServiceBusMessage(JsonConvert.SerializeObject(new CheckOrderPaymentStatus { Id = id, RetryAttempt = retryAttempt }));
        var seq = await sender.ScheduleMessageAsync(message, DateTimeOffset.Now.Add(delayedTimeSpan));

        await sender.CancelScheduledMessageAsync(seq);
        return true;
    }

    private static Order GetOrderById(int orderId)
    {
        return new Faker<Order>().RuleFor(o => o.Id, orderId);
    }

    private static string CheckPaymentStatus(Order order)
    {
        // re-trieve order payment status from payment gateway side
        int randomNumber = random.Next(2);
        string result = randomNumber == 0 ? "success" : "processing";
        return result;
    }
}
