using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace FaceSender
{
    public static class NewOrderFunction
    {
        [FunctionName("NewOrderFunction")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var orderDetails = JsonConvert.DeserializeObject<OrderDetails>(requestBody);

            return orderDetails != null
                ? (ActionResult)new OkObjectResult("Order saved.")
                : new BadRequestObjectResult("Please pass a valid order in the request body");
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
