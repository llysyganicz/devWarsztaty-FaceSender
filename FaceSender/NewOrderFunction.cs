using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;

namespace FaceSender
{
    public static class NewOrderFunction
    {
        [FunctionName("NewOrderFunction")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, 
            [Table("Order", Connection = "StorageConnection")]ICollector<OrderDetails> orders,
            TraceWriter log)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var orderDetails = JsonConvert.DeserializeObject<OrderDetails>(requestBody);

            if (orderDetails != null)
            {
                orders.Add(orderDetails);
                return new OkObjectResult("Order saved.");
            }
            return new BadRequestObjectResult("Please pass a valid order in the request body");
        }
    }

    public class OrderDetails
    {
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public int PhotoWidth { get; set; }
        public int PhotoHeight { get; set; }
        public string PhotoName { get; set; }
    }
}
