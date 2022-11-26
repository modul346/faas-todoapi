using Microsoft.Azure.WebJobs;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Linq;

namespace Functions
{
    public static class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient tableClient,
            ILogger log)
        {
            var queryResults = tableClient.Query<TodoTableEntity>(filter: $"PartitionKey eq 'TODO'");
            var page = queryResults.AsPages(null).First();

            var deleted = 0;
            foreach (var todo in page.Values)
            {
                if (todo.IsCompleted)
                {
                    tableClient.DeleteEntity("TODO", todo.RowKey);
                    deleted++;
                }
            }
            log.LogInformation($"Deleted {deleted} items at {DateTime.Now}");
        }
    }
}
