using System.Collections.Generic;

namespace ServiceBusDelayedProcessing;

public class SampleDbContext
{
    // NOT A REAL DBCONTEXT
    public List<Order> Orders { get; set; } = new List<Order>();
    public void SaveChanges()
    {
        // dbContext.SaveChanges();
    }
}