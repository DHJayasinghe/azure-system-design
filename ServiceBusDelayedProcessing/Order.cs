using System;

namespace ServiceBusDelayedProcessing;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public int CustomerId { get; set; }
    public string OrderDate { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int Attempt { get; internal set; }
    public DateTime CompletedDateTime { get; internal set; }

    internal bool CanComplete()
    {
        return true;
    }

    internal void Complete()
    {
        Status = "COMPLETE";
    }

    internal void MarkAsPaymentPending()
    {
        Status = "PAYMENT_PENDING";
    }
}