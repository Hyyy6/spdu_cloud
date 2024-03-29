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
        private static bool _authenticate(ReqMin request)
        {
            // string name = request.name;
            string pwd = request.password;
            byte[] hashed_data;
            string hashed_pwd;
            
            if (String.IsNullOrWhiteSpace(pwd))
                return false;

            using (SHA256 hasher = SHA256.Create())
            {
                hashed_data = hasher.ComputeHash(Encoding.ASCII.GetBytes(pwd));
                hashed_pwd = BitConverter.ToString(hashed_data);
            }
            // SPDUAPI.log.LogInformation("pwd = {0}", pwd);
            // SPDUAPI.log.LogInformation("hashed_pwd = {0}", hashed_pwd);
            var secPass = Environment.GetEnvironmentVariable("password");

            if (String.Compare(secPass, hashed_pwd) == 0)
            {
                SPDUAPI.log.LogInformation("Successful authentitication for ard.");
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
                
                if (!_validateIP(request.ipAddress))
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

            // SPDUPayload payload = request.payload;
            int stateLength = 0;
            BlobEntry data = new BlobEntry();
            data.ipAddress = request.ipAddress;
            data.date = DateTime.Now.ToString();
            // data.key = Encoding.ASCII.GetBytes(request.key);//request.key;
            data.key = request.key;
            try {
                stateLength = request.state.Length;
                data.state = new int[stateLength];
                Array.Copy(request.state, data.state, stateLength);
            } catch (Exception e) {
                SPDUAPI.log.LogInformation("Bad request: {0}.", e.ToString());
                ObjectResult result = new ObjectResult("Bad state in request");
                return result;
            }
            CloudBlockBlob cloudBlob = storageBlob.GetBlockBlobReference("arduino");

            await cloudBlob.UploadTextAsync(JsonSerializer.Serialize(data));
            SPDUAPI.log.LogInformation("Put data req processed.");
            return new OkObjectResult(String.Format("Updated ard local IP address to {0}", request.ipAddress));

        }

        private static bool _validateGetData(object reqData, out ReqMin request)
        {
            try
            {
                request = JsonSerializer.Deserialize<ReqMin>(Convert.ToString(reqData));
                
                // if (!_authenticate(request))
                //     return false;
                
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
            ReqMin request;
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

            // SPDUPayload payload = request.payload;
            CloudBlockBlob cloudBlob = storageBlob.GetBlockBlobReference("arduino");

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