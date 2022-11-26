using Microsoft.Azure.WebJobs;
using Azure.Storage.Blobs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using Azure.Storage.Blobs.Models;

namespace Functions
{
    public static class QueueListeners
    {
        [FunctionName("QueueListeners")]
        public static async Task Run([QueueTrigger("todos", Connection = "AzureWebJobsStorage")] ToDo todo,
            [Blob("todos", Connection = "AzureWebJobsStorage")] BlobContainerClient containerClient,
            ILogger log)
        {
            
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
            var blob =  containerClient.GetBlobClient($"{todo.Id}.txt");

            var text = $"Created a new task: {todo.TaskDescription}";
            var byteArray = Encoding.UTF8.GetBytes(text);
            var stream = new MemoryStream(byteArray);

            await blob.UploadAsync(stream,new BlobHttpHeaders() {ContentType = "text"} );
            log.LogInformation($"C# Queue trigger function processed: {todo.TaskDescription}");
        }
    }
}
