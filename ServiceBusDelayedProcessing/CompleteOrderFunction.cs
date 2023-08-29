using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Bogus;

namespace ServiceBusDelayedProcessing;

public class CompleteOrderFunction
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly SampleDbContext _dbContext;
    private readonly static Random random = new();

    public CompleteOrderFunction(ServiceBusClient serviceBusClient, SampleDbContext dbContext)
    {
        _serviceBusClient = serviceBusClient;
        _dbContext = dbContext;
    }

    [FunctionName(nameof(CompleteOrderFunction))]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "order/complete")] OrderCompleteRequest req)
    {
        int id = req.Id;

        var order = GetOrderById(id);
        var canComplete = order.CanComplete();
        if (!canComplete) 
            return new BadRequestObjectResult("Order cannot be completed");

        order.Complete();

        string paymentStatus = ProcessPayment(order);
        if (paymentStatus == "processing")
        {
            order.MarkAsPaymentPending();
            await CheckPaymentStatusLaterAsync(order.Id, 1);
        }
        _dbContext.SaveChanges();

        return new OkResult();
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
        return true;
    }

    private static Order GetOrderById(int orderId)
    {
        return new Faker<Order>().RuleFor(o => o.Id, orderId);
    }

    private static string ProcessPayment(Order order)
    {
        // re-trieve customer infor related to order
        // and call related payment gateway provider
        int randomNumber = random.Next(2);
        string result = randomNumber == 0 ? "success" : "processing";
        return result;
    }
}

public record CheckOrderPaymentStatus
{
    public int Id { get; set; }
    public int RetryAttempt { get; set; }
}

public record OrderCompleteRequest
{
    public int Id { get; set; }
}