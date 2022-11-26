using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Azure.Data.Tables;
using Azure;
//using Microsoft.WindowsAzure.Storage.Table;

namespace Functions
{
    public static class ToDoApi
    {
        [FunctionName("CreateToDo")]
        public static async Task<IActionResult> CreateToDo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todos")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient tableClient,
            [Queue("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<ToDo> todoQueue,
            ILogger log)
        {
            log.LogInformation("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<ToDoCreateModel>(requestBody);

            var todo = new ToDo() { TaskDescription = input.TaskDescription };
            tableClient.AddEntity(todo.ToTableEntity());
            await todoQueue.AddAsync(todo);
            return new OkObjectResult(todo);
        }

        [FunctionName("GetToDos")]
        public static IActionResult GetToDos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient tableClient, 
            ILogger log)
        {
            log.LogInformation("Getting todo list items");
            var queryResults = tableClient.Query<TodoTableEntity>(filter: $"PartitionKey eq 'TODO'");
            var page =  queryResults.AsPages(null).First();

            return new OkObjectResult(page.Values.Select(Mappings.ToTodo));
        }

        [FunctionName("GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos/{id}")] HttpRequest req,
            [Table("todos", "TODO", "{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo,
            ILogger log, string id)
        {

            if (todo == null)
            {
                log.LogInformation($"Could not find a todo with the id {id}");
                return new NotFoundResult();
            }
            return new OkObjectResult(todo);
        }

        [FunctionName("UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todos/{id}")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient tableClient,
            ILogger log, string id)
        {
            log.LogInformation($"Updating todo with the id {id}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var findResult = tableClient.GetEntity<TodoTableEntity>("TODO", id);
            var updated = JsonConvert.DeserializeObject<ToDoUpdateModel>(requestBody);

            if (findResult.Value == null)
            {
                log.LogInformation($"Could not find a todo with the id {id} to update");
                return new NotFoundResult();
            }

            var existingRow = (TodoTableEntity)findResult.Value;
            existingRow.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                existingRow.TaskDescription = updated.TaskDescription;
            }

            tableClient.UpdateEntity(existingRow, ifMatch: ETag.All);
            return new OkObjectResult(existingRow.ToTodo());
        }

        [FunctionName("DeleteTodo")]
        public static IActionResult DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todos/{id}")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] TableClient tableClient,
            ILogger log, string id)
        {
            log.LogInformation($"Deleting todo with the id {id}");


            var deleteResult = tableClient.DeleteEntity("TODO", id);
            if (deleteResult == null)
            {
                log.LogInformation($"Could not find a todo with the id {id} to delete");
                return new NotFoundResult();
            }

            return new OkResult();
        }
    }
}
