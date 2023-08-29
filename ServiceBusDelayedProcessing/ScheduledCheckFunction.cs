using System;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ServiceBusDelayedProcessing
{
    public class ScheduledCheckFunction
    {
        private readonly SampleDbContext _dbContext;

        public ScheduledCheckFunction(SampleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [FunctionName("ScheduledCheckFunction")]
        public void Run([TimerTrigger("0 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var firstAttemptOrders = _dbContext.Orders
                .Where(o => o.Status == "PAYMENT_PENDING" 
                    && o.Attempt == 0 
                    && DateTime.UtcNow.Subtract(o.CompletedDateTime).TotalHours >= 1);
            var secondAttemptOrders = _dbContext.Orders
                .Where(o => o.Status == "PAYMENT_PENDING" 
                    && o.Attempt == 1 
                    && DateTime.UtcNow.Subtract(o.CompletedDateTime).TotalHours >= 6);
            var thirdAttemptOrders = _dbContext.Orders
                .Where(o => o.Status == "PAYMENT_PENDING" 
                    && o.Attempt == 2 
                    && DateTime.UtcNow.Subtract(o.CompletedDateTime).TotalHours >= 24);

            // Payment Gateway status check logic removed for brevity
        }
    }
}
