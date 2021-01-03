using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
// using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Logging;
using api;

namespace shared
{
    public static class EnvironmentVariables 
    {
        private static readonly string _storeNameVar = "AzureWebJobsStorage";
        
        public static string connectionString
        {
            get => Environment.GetEnvironmentVariable(_storeNameVar);
        }
        //;// _storageEnvVarName ; // = "test";
        // public static string StorageConnectionString => Environment.GetEnvironmentVariable(_stor);
        // public static string getStoreConnectionString => _stor;
    }

    public static class BlobRoutine
    {
        private static bool _authenticate(string pwd)
        {
            if (pwd is null)
                return false;

            spduAPI.log.LogInformation("auth " + pwd);
            // SecureString secureString;
            var secPass = Environment.GetEnvironmentVariable("secret_uri");
            spduAPI.log.LogInformation(secPass);
            return true;
        }
        public static async Task<IActionResult> putData(dynamic reqData, string name, CloudBlobContainer storageBlob)
        {
            name = name ?? reqData?.name;
            // string responseMessage = string.IsNullOrEmpty(name)
                // ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                // : $"Hello, {name}. This HTTP triggered function executed successfully.";
            
            spduAPI.log.LogInformation("Put data for " + name);
            
            string pwd = reqData?.pwd;
            if (pwd is null)
            {
                ObjectResult result = new ObjectResult("Wrong pwd.");
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }
            _authenticate(pwd);
            string devName = reqData?.dev;
            if (devName is null)
            {
                ObjectResult result = new ObjectResult("Wrong pwd.");
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }
            spduAPI.log.LogInformation(devName);
            CloudBlockBlob cloudBlob = storageBlob.GetBlockBlobReference(devName);
            // cloudBlob.cre
            // if (await cloudBlob.ExistsAsync())
            // {
                //if (!_parseReqData(reqData.data))
                // spduAPI.log.LogInformation(reqData.ToString());
            dynamic devData = reqData?.data;
            string localIP = devData?.ip;
            spduAPI.log.LogInformation(localIP);
            string devDataStr = devData.ToString();
            spduAPI.log.LogInformation(devDataStr);
            byte[] _toStream = Encoding.UTF8.GetBytes(devDataStr);
            spduAPI.log.LogInformation(BitConverter.ToString(_toStream));
            // using (MemoryStream ms = new MemoryStream(_toStream))
            // {
                spduAPI.log.LogInformation("Upload to " + localIP);
                // spduAPI.log.LogInformation(ms.
                // await cloudBlob.UploadFromStreamAsync(ms);
                Task ret = cloudBlob.UploadTextAsync("123");
                await ret;
                return new OkObjectResult("Ok");
            // }
            // }
            // else
            {
                // ObjectResult result = new ObjectResult()
            }
        }

        public static async Task<IActionResult> getData(dynamic reqData, string name, CloudBlobContainer storageBlob)
        {
            name = name ?? reqData?.name;
            spduAPI.log.LogInformation("Get data for " + name);

            string pwd = reqData?.pwd;
            spduAPI.log.LogInformation(pwd);
            if (pwd is null)
            {
                ObjectResult result = new ObjectResult("Wrong pwd.");
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }
            _authenticate(pwd);
            string devName = reqData?.dev;
            if (devName is null)
            {
                ObjectResult result = new ObjectResult("Wrong pwd.");
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }
            spduAPI.log.LogInformation(devName);
            CloudBlockBlob cloudBlob = storageBlob.GetBlockBlobReference(devName);

            if (await cloudBlob.ExistsAsync())
            {
                spduAPI.log.LogInformation("download from blob");
                var ms = new MemoryStream();
                await cloudBlob.DownloadToStreamAsync(ms);
                spduAPI.log.LogInformation(ms.Length.ToString());
                string retData = new StreamReader(ms).ReadToEnd();
                spduAPI.log.LogInformation(retData);
                return new OkObjectResult(retData);
            }
            else
            {
                ObjectResult result = new ObjectResult("No data for this device.");
                result.StatusCode = StatusCodes.Status404NotFound;
                return result;
            }
        }
    }
}