using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace LunchDutyFunction
{
    public static class Function
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 0 1 * * 1-5")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
