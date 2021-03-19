using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using utils;
using System.Security.Cryptography;


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

            IActionResult result;
            string requestBody;
            int contentLength;
            dynamic reqData;

            log = _log;
            log.LogInformation("Processed {0} HTTP request.", req.Method, req);

            await storageBlob.CreateIfNotExistsAsync();

            // var dict = new Dictionary(req.Headers)
            // log.LogInformation("Headers: " +);
            switch (req.Method) {
                case "PUT":
                    byte[] reqBody = {};
                    byte[] iv = new byte[16];
                    // string requestBody = await new StreamReader(req.Body).ReadAsync();
                    string tmp = "Content-Length";
                    log.LogInformation(req.Headers[tmp]);
                    if (!req.Headers.ContainsKey("Content-Length") || req.ContentLength <= 0) {
                        log.LogInformation("Invalid content length.");
                        return new BadRequestObjectResult("Invalid content length.");
                    } else {
                        contentLength = (int)req.ContentLength;
                    }
                    try {
                        reqBody = new byte[contentLength - 16];
                        await req.Body.ReadAsync(reqBody, 0, contentLength - 16);
                        await req.Body.ReadAsync(iv, 0, 16);
                    } catch (Exception e) {
                        log.LogInformation(e.Message);
                    }

                    log.LogInformation("request length - {0}", contentLength);
                    log.LogInformation(Encoding.ASCII.GetString(reqBody, 0, contentLength - 16));

                    using (Aes cypher = Aes.Create())
                    {
                        // string key = "abcdefghijklmnop";
                        string key = System.Environment.GetEnvironmentVariable("secret");
                        byte[] buf = reqBody;
                        string decrypted = DecryptStringFromBytes_Aes(buf, Encoding.ASCII.GetBytes(key), iv);
                        log.LogInformation(decrypted);
                        requestBody = decrypted;
                    }
            
                    reqData = JsonConvert.DeserializeObject(requestBody);

                    result = await BlobRoutine.putData(reqData, storageBlob);
                    return new OkObjectResult(result);
                
                case "POST":
                    if (req.ContentLength <= 0)
                    {
                        log.LogInformation("Invalid content length.");
                        return new BadRequestObjectResult("Invalid content length.");
                    }
                    else
                    {
                        contentLength = (int)req.ContentLength;
                    }

                    requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    log.LogInformation(requestBody);
                    reqData = JsonConvert.DeserializeObject(requestBody);
                    result = await BlobRoutine.getData(reqData, storageBlob);
                    return new OkObjectResult(result);

                case "GET":
                default:
                    log.LogInformation("Wrong HTTP request type.");
                    return new BadRequestObjectResult("Wrong HTTP request");
                    
            }
            
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Clear();
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.BlockSize = 128;
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                log.LogInformation(Encoding.ASCII.GetString(aesAlg.Key));
                log.LogInformation(Encoding.ASCII.GetString(aesAlg.IV));
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    byte[] pre_cipher = msDecrypt.ToArray();
                    log.LogInformation("pre cipher length - {0}", pre_cipher.Length);
                    log.LogInformation(Encoding.ASCII.GetString(pre_cipher));

                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using(StreamReader srDecrypt = new StreamReader(csDecrypt)) {
                            plaintext = srDecrypt.ReadToEnd();
                            log.LogInformation(plaintext);
                        }

                    }
                }
            }

            return plaintext;
        }
    }
}
