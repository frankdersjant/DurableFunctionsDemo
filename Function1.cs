using DurableFunctionsDemo.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DurableFunctionsDemo
{
    public static class Function1
    {
        [Function("O_ProcessOrder")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Function1));
            logger.LogInformation("Saying hello.");

            var order = context.GetInput<Order>();
            var orderDate = await context.CallActivityAsync<string>(nameof(A_CheckOrder), order);

            return "Order processed";
        }

        [Function(nameof(A_CheckOrder))]
        public static async Task<string> A_CheckOrder([ActivityTrigger] Order OrderData)
        {
            string orderprocessed = "Order" + OrderData.OrderId + "order date checked at " + DateTime.Now.ToString();
            return "order processed";
        }

        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var order = JsonConvert.DeserializeObject<Order>(requestBody.ToString());

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("O_ProcessOrder", order);
            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
