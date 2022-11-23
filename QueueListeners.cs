using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Functions
{
    public static class QueueListeners
    {
        [FunctionName("QueueListeners")]
        public static async Task Run([QueueTrigger("todos", Connection = "AzureWebJobsStorage")] ToDo todo,
            [Blob("todos", Connection = "AzureWebJobsStorage")] CloudBlobContainer container,
            ILogger log)
        {
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference($"{todo.Id}.txt");
            await blob.UploadTextAsync($"Created a new task: {todo.TaskDescription}");
            log.LogInformation($"C# Queue trigger function processed: {todo.TaskDescription}");
        }
    }
}
