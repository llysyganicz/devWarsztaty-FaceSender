
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace FaceSender
{
    public static class ResizePictureFunction
    {
        [FunctionName("ResizePictureFunction")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, 
            [Blob("photos", FileAccess.Read, Connection = "StorageConnection")]CloudBlobContainer photosContainer,
            [Blob("doneorders/{rand-guid}", FileAccess.ReadWrite, Connection = "StorageConnection")]ICloudBlob resizedPhotosCloudBlob,
            TraceWriter log)
        {
            var request = GetPictureResizeRequest(req);
            var photoStream = await GetSourcePhotoStream(photosContainer, request.FileName);
            SetAttachmentContentDisposition(resizedPhotosCloudBlob, request);

            var image = Image.Load(photoStream);
            image.Mutate(x => x.Resize(request.Width, request.Height));

            var resizedPhotoStream = new MemoryStream();
            image.Save(resizedPhotoStream, new JpegEncoder());

            await resizedPhotosCloudBlob.UploadFromStreamAsync(resizedPhotoStream);

            return new JsonResult(new { FileName = resizedPhotosCloudBlob.Name });
        }

        private static PictureResizeRequest GetPictureResizeRequest(HttpRequest req)
        {
            var requestBody = new StreamReader(req.Body).ReadToEnd();
            var request = JsonConvert.DeserializeObject<PictureResizeRequest>(requestBody);

            return request;
        }

        private async static Task<Stream> GetSourcePhotoStream(CloudBlobContainer photoContainer, string fileName)
        {
            var photoBlob = await photoContainer.GetBlobReferenceFromServerAsync(fileName);
            var photoStream = await photoBlob.OpenReadAsync(AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions(), new OperationContext());
            return photoStream;
        }

        private static void SetAttachmentContentDisposition(ICloudBlob resizedPhotoCloudBlob, PictureResizeRequest request)
        {
            resizedPhotoCloudBlob.Properties.ContentDisposition = $"attachment; filename={request.Width}x{request.Height}.jpg";
        }
    }

    public class PictureResizeRequest
    {
        public string FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
