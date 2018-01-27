using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace FaceSender
{
    public static class RetrieveOrderFunction
    {
        [FunctionName("RetrieveOrderFunction")]
        public async static Task<IActionResult> Run([BlobTrigger("photos/{name}", Connection = "StorageConnection")]Stream myBlob, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnection"]);
            var tableClient = storageAccount.CreateCloudTableClient();
            var orderTable = tableClient.GetTableReference("Order");
            var query = new TableQuery<OrderDetails>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, name));
            var result = await orderTable.ExecuteQuerySegmentedAsync<OrderDetails>(query, null);
            var tableResult = result.Results;

            if(tableResult.Any())
            {
                var first = tableResult.First();
                return new JsonResult(new
                {
                    first.CustomerEmail,
                    first.CustomerName,
                    first.PhotoHeight,
                    first.PhotoWidth,
                    first.PhotoName
                });
            }

            return new NotFoundResult();
        }
    }
}
