using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Security.Cryptography;
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
    }

    public static class BlobRoutine
    {
        private static bool _authenticate(string pwd)
        {
            byte[] hashed_data;
            string hashed_pwd;
            
            if (pwd is null)
                return false;

            using (SHA256 hasher = SHA256.Create())
            {
                hashed_data = hasher.ComputeHash(Encoding.ASCII.GetBytes(pwd));
                hashed_pwd = BitConverter.ToString(hashed_data);
                spduAPI.log.LogInformation("auth - " + pwd + "\nhashed pass - " + hashed_pwd);
            }
            
            var secPass = Environment.GetEnvironmentVariable("secret");
            if (String.Compare(secPass, hashed_pwd) == 0)
            {
                spduAPI.log.LogInformation("Successful authentitication.");
                return true;
            }
            spduAPI.log.LogInformation(secPass);
            return false;
        }

        private static bool _validateData(dynamic reqData, out ObjectResult res)
        {
            string name = reqData?.name;
            string pwd = reqData?.pwd;
            if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(pwd))
            {
                res = new ObjectResult("Invalid credentials.");
                res.StatusCode = StatusCodes.Status400BadRequest;
                return false;
            }

            res = new ObjectResult("Ok");
            return true;
        }
        public static async Task<IActionResult> putData(dynamic reqData, CloudBlobContainer storageBlob)
        {
            string name = reqData?.name;      
            spduAPI.log.LogInformation("Put data for " + name);
            
            string pwd = reqData?.pwd;
            if (pwd is null || !_authenticate(pwd))
            {
                ObjectResult result = new ObjectResult("Could not authenticate.");
                result.StatusCode = StatusCodes.Status403Forbidden;
                return result;
            }

            dynamic payload = reqData?.data;
            string devName = payload?.dev;
            if (devName is null)
            {
                ObjectResult result = new ObjectResult("Check your device name");
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }
            spduAPI.log.LogInformation(devName);
            CloudBlockBlob cloudBlob = storageBlob.GetBlockBlobReference(devName);

            string localIP = payload?.ip;
            spduAPI.log.LogInformation(localIP);
            string devDataStr = payload.ToString();
            spduAPI.log.LogInformation(devDataStr);
            byte[] _toStream = Encoding.UTF8.GetBytes(devDataStr);
            spduAPI.log.LogInformation(BitConverter.ToString(_toStream));

            spduAPI.log.LogInformation("Upload new local IP " + localIP + " for " + devName);

            await cloudBlob.UploadTextAsync(localIP);
            return new OkObjectResult("Ok");

        }

        public static async Task<IActionResult> getData(dynamic reqData, CloudBlobContainer storageBlob)
        {
            string name = reqData?.name;
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
            dynamic payload = reqData?.data;
            string devName = payload?.dev;
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
                ms.Seek(0, SeekOrigin.Begin);
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