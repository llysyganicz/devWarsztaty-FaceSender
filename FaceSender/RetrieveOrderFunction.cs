using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace FaceSender
{
    public static class RetrieveOrderFunction
    {
        [FunctionName("RetrieveOrderFunction")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
            [Table("Orders", Connection = "StorageConnection")]CloudTable ordersTable, TraceWriter log)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrWhiteSpace(fileName))
                return new BadRequestResult();
            TableQuery<OrderDetails> query = new TableQuery<OrderDetails>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, fileName));
            TableQuerySegment<OrderDetails> tableQueryResult = await ordersTable.ExecuteQuerySegmentedAsync(query, null);
            var resultList = tableQueryResult.Results;

            if (resultList.Any())
            {
                var firstElement = resultList.First();

                var resolutions = firstElement.Resolutions.Split(',');
                var requests = new List<PictureResizeRequest>();

                foreach (var resolution in resolutions)
                {
                    var resParams = resolution.Split('x');
                    requests.Add(new PictureResizeRequest
                    {
                        FileName = firstElement.PhotoName,
                        Width = int.Parse(resParams[0]),
                        Height = int.Parse(resParams[1])
                    });
                }
                return new JsonResult(new { requests, firstElement.CustomerEmail });
            }

            return new NotFoundResult();
        }
    }
}
