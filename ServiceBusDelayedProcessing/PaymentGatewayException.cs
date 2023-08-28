using System;

namespace ServiceBusDelayedProcessing;

internal class PaymentGatewayException : Exception
{
    public PaymentGatewayException()
    {
    }

    public PaymentGatewayException(string message) : base(message)
    {
    }
}