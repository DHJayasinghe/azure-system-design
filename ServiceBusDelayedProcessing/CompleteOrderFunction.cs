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

    public CompleteOrderFunction(ServiceBusClient serviceBusClient, SampleDbContext dbContext)
    {
        _serviceBusClient = serviceBusClient;
        _dbContext = dbContext;
    }

    [FunctionName(nameof(CompleteOrderFunction))]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "order/complete")] OrderCompleteRequest req)
    {
        int id = req.Id;

        var order = GetOrderById(id);
        var canComplete = order.CanComplete();
        if (!canComplete) return new BadRequestObjectResult("Order cannot be completed");

        order.Complete();
        bool paymentSuccess = ProcessPayment(order);
        if (!paymentSuccess)
        {
            order.MarkAsPaymentPending();
        }
        _dbContext.SaveChanges();

        return new OkResult();
    }

    private static Order GetOrderById(int orderId)
    {
        return new Faker<Order>().RuleFor(o=>o.Id, orderId);
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

    private async Task CheckPaymentStatusLaterAsync(int id, int retryAttempt)
    {
        await using var sender = _serviceBusClient.CreateSender("order");
        var message = new ServiceBusMessage(JsonConvert.SerializeObject(new CheckOrderPaymentStatus { Id = id, RetryAttempt = retryAttempt }));
        var seq = await sender.ScheduleMessageAsync(message, DateTimeOffset.Now.AddMinutes(3));
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