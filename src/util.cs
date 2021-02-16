using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using API;
using objectModels;

namespace utils
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
        private static bool _authenticate(SPDURequest request)
        {
            string name = request.name;
            string pwd = request.password;
            byte[] hashed_data;
            string hashed_pwd;
            
            if (String.IsNullOrWhiteSpace(name) || String.IsNullOrWhiteSpace(pwd))
                return false;

            using (SHA256 hasher = SHA256.Create())
            {
                hashed_data = hasher.ComputeHash(Encoding.ASCII.GetBytes(pwd));
                hashed_pwd = BitConverter.ToString(hashed_data);
            }
            
            var secPass = Environment.GetEnvironmentVariable("secret");
            if (String.Compare(secPass, hashed_pwd) == 0)
            {
                SPDUAPI.log.LogInformation("Successful authentitication for {0}.", name);
                return true;
            }
            return false;
        }

        private static bool _validateIP(string ipAddress)
        {
            if (String.IsNullOrEmpty(ipAddress))
                return false;

            
            var octets = ipAddress.Split('.');
            if (octets.Length != 4)
                return false;

            foreach (string octet in octets)
            {
                int num;
                Int32.TryParse(octet, out num);
                if (num < 0 || num > 255)
                    return false;
            }
            return true;
        }

        private static bool _validatePutData(object reqData, out SPDURequest request)
        {
            try
            {
                request = JsonSerializer.Deserialize<SPDURequest>(Convert.ToString(reqData));
                if (String.IsNullOrEmpty(request.payload.deviceName) || !_validateIP(request.payload.ipAddress))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                request = null;
                SPDUAPI.log.LogInformation(e.ToString());
                return false;

            }
        }
        public static async Task<IActionResult> putData(dynamic reqData, CloudBlobContainer storageBlob)
        {
            SPDURequest request;
            if (!_validatePutData(reqData, out request))
            {
                ObjectResult result = new ObjectResult("Could not parse input.");
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }

            if (!_authenticate(request))
            {
                ObjectResult result = new ObjectResult("Could not authenticate.");
                result.StatusCode = StatusCodes.Status403Forbidden;
                return result;
            }

            SPDUPayload payload = request.payload;

            CloudBlockBlob cloudBlob = storageBlob.GetBlockBlobReference(payload.deviceName);

            await cloudBlob.UploadTextAsync(payload.ipAddress + "\ntime: " + DateTime.Now.ToString());
            SPDUAPI.log.LogInformation("Put data req processed.");
            return new OkObjectResult(String.Format("Updated {0} local IP address to {1}", payload.deviceName, payload.ipAddress));

        }

        private static bool _validateGetData(object reqData, out SPDURequest request)
        {
            try
            {
                request = JsonSerializer.Deserialize<SPDURequest>(Convert.ToString(reqData));
                if (String.IsNullOrEmpty(request.payload.deviceName))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                request = null;
                SPDUAPI.log.LogInformation(e.ToString());
                return false;

            }
        }
        public static async Task<IActionResult> getData(dynamic reqData, CloudBlobContainer storageBlob)
        {
            SPDURequest request;
            if (!_validateGetData(reqData, out request))
            {
                ObjectResult result = new ObjectResult("Could not parse input.");
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }

            if (!_authenticate(request))
            {
                ObjectResult result = new ObjectResult("Could not authenticate.");
                result.StatusCode = StatusCodes.Status403Forbidden;
                return result;
            }

            SPDUPayload payload = request.payload;
            CloudBlockBlob cloudBlob = storageBlob.GetBlockBlobReference(payload.deviceName);

            if (await cloudBlob.ExistsAsync())
            {
                var ms = new MemoryStream();
                await cloudBlob.DownloadToStreamAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                string retData = new StreamReader(ms).ReadToEnd();
                SPDUAPI.log.LogInformation("Get data req processed.");
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