using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FaceSender
{
    public static class DurableResizePictureFunction
    {
        [FunctionName("DurableResizePictureFunction")]
        public static async Task<string[]> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var pictureResizeRequests = context.GetInput<PictureResizeRequest[]>();

            var tasks = new Task<string>[pictureResizeRequests.Length];
            for (int i = 0; i < pictureResizeRequests.Length; i++)
            {
                tasks[i] = context.CallActivityAsync<string>(
                "DurableResizePictureFunction_ResizePicture",
                pictureResizeRequests[i]);
            }

            await Task.WhenAll(tasks);

            string[] resizedPicturesNames = new string[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                resizedPicturesNames[i] = tasks[i].Result;
            }
            return resizedPicturesNames;
        }

        [FunctionName("DurableResizePictureFunction_ResizePicture")]
        public static async Task<string> ResizePicture([ActivityTrigger] PictureResizeRequest pictureResizeRequest,
            [Blob("photos", FileAccess.Read, Connection = "StorageConnection")]CloudBlobContainer photosContainer,
            [Blob("doneorders/{rand-guid}", FileAccess.ReadWrite, Connection = "StorageConnection")]ICloudBlob resizedPhotoCloudBlob,
            TraceWriter log)
        {
            var photoStream = await GetSourcePhotoStream(photosContainer, pictureResizeRequest.FileName);
            SetAttachmentAsContentDisposition(resizedPhotoCloudBlob, pictureResizeRequest);

            var image = Image.Load(photoStream);
            image.Mutate(e => e.Resize(pictureResizeRequest.Width, pictureResizeRequest.Height));

            var resizedPhotoStream = new MemoryStream();
            image.Save(resizedPhotoStream, new JpegEncoder());
            resizedPhotoStream.Seek(0, SeekOrigin.Begin);

            await resizedPhotoCloudBlob.UploadFromStreamAsync(resizedPhotoStream);

            return resizedPhotoCloudBlob.Name;
        }


        [FunctionName("DurableResizePictureFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            var content = req.Content;
            string jsonContent = await content.ReadAsStringAsync();
            dynamic pictureResizeRequests = JsonConvert.DeserializeObject<PictureResizeRequest[]>(jsonContent);

            string instanceId = await starter.StartNewAsync("DurableResizePictureFunction", pictureResizeRequests);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        private static void SetAttachmentAsContentDisposition(ICloudBlob resizedPhotoCloudBlob,
            PictureResizeRequest pictureResizeRequest)
        {
            resizedPhotoCloudBlob.Properties.ContentDisposition =
                $"attachment; filename={pictureResizeRequest.Width}x{pictureResizeRequest.Height}.jpeg";
        }

        private static async Task<Stream> GetSourcePhotoStream(CloudBlobContainer photosContainer,
            string fileName)
        {
            var photoBlob = await photosContainer.GetBlobReferenceFromServerAsync(fileName);
            var photoStream = await photoBlob.OpenReadAsync(AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(), new OperationContext());
            return photoStream;
        }

    }
}