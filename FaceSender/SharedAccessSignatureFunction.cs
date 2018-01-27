
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace FaceSender
{
    public static class SharedAccessSignatureFunction
    {
        [FunctionName("SharedAccessSignatureFunction")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
            [Blob("doneorders", FileAccess.Read, Connection = "StorageConnection")]CloudBlobContainer photosContainer,
            TraceWriter log)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrWhiteSpace(fileName)) return new BadRequestResult();

            var photoBlob = await photosContainer.GetBlobReferenceFromServerAsync(fileName);

            var photoUrl = GetBlobSasUri(photoBlob);

            return new JsonResult(new { PhotoUrl = photoUrl });
        }

        private static string GetBlobSasUri(ICloudBlob blob)
        {
            var sasConstrains = new SharedAccessBlobPolicy();
            sasConstrains.SharedAccessStartTime = DateTimeOffset.UtcNow.AddHours(-1);
            sasConstrains.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
            sasConstrains.Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read;

            string sasToken = blob.GetSharedAccessSignature(sasConstrains);

            return blob.Uri + sasToken;
        }
    }
}
