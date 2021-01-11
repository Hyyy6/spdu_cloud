using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using utils;

namespace API
{
    public static class SPDUAPI
    {
        public static ILogger log {get; private set;}
        [FunctionName("spduAPI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = null)] HttpRequest req,
            [Blob("test", Connection = "AzureWebJobsStorage")] CloudBlobContainer storageBlob,
            ILogger _log)
        {
            if (!req.IsHttps)
            {
                return new BadRequestResult();
            }

            IActionResult result;
            log = _log;
            log.LogInformation("Processed {0} HTTP request.", req.Method, req);

            await storageBlob.CreateIfNotExistsAsync();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var reqData = JsonConvert.DeserializeObject(requestBody);

            switch (req.Method) {
                case "PUT":
                    result = await BlobRoutine.putData(reqData, storageBlob);
                    return new OkObjectResult(result);
                
                case "GET":
                case "POST":
                    result = await BlobRoutine.getData(reqData, storageBlob);
                    return new OkObjectResult(result);

                default:
                    log.LogInformation("Wrong HTTP request type.");
                    return new BadRequestObjectResult("Wrong HTTP request");
                    
            }
            
        }
    }
}
