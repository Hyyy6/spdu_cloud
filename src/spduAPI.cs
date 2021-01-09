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
using shared;

namespace api
{
    public static class spduAPI
    {
        public static ILogger log {get; private set;}
        // private static Boolean _authenticate( data) {
        //     return true;
        // }
        [FunctionName("spduAPI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, Route = null)] HttpRequest req,
            [Blob("test", Connection = "AzureWebJobsStorage")] CloudBlobContainer storageBlob,
            ILogger _log)
        {
            IActionResult result;
            log = _log;
            log.LogInformation("C# HTTP trigger function processed a request.");
            // log.LogInformation(EnvironmentVariables.getStoreConnectionString);

            // if (CloudStorageAccount.TryParse())
            await storageBlob.CreateIfNotExistsAsync();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var reqData = JsonConvert.DeserializeObject(requestBody);
            string name = req.Query["name"];
            // string responseMessage = "Ok";
            log.LogInformation(req.Method);

            switch (req.Method) {
                case "PUT":
                    result = await BlobRoutine.putData(reqData, name, storageBlob);
                    return new OkObjectResult(result);
                    // break;
                
                case "GET":
                case "POST":
                    result = await BlobRoutine.getData(reqData, name, storageBlob);
                    return new OkObjectResult(result);
                    // break;

                default:
                    log.LogInformation("Wrong HTTP request type.");
                    return new BadRequestObjectResult("Wrong HTTP request");
                    // break;
                    
            }
            

            // return new OkObjectResult(responseMessage);
        }
    }
}
