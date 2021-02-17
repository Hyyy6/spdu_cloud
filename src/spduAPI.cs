using System;
using System.Text;
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
            // if (!req.IsHttps)
            // {
            //     return new BadRequestResult();
            // }

            IActionResult result;
            log = _log;
            log.LogInformation("Processed {0} HTTP request.", req.Method, req);

            await storageBlob.CreateIfNotExistsAsync();
            byte[] reqBody = {};
            string requestBody;
            int contentLength;
            // string requestBody = await new StreamReader(req.Body).ReadAsync();
            if (req.ContentLength <= 0) {
                log.LogInformation("Invalid content length.");
                return new BadRequestObjectResult("Invalid content length.");
            } else {
                contentLength = (int)req.ContentLength;
            }
            try {
                reqBody = new byte[contentLength];
                await req.Body.ReadAsync(reqBody, 0, contentLength);
            } catch (Exception e) {
                log.LogInformation(e.Message);
            }

            log.LogInformation("request length - {0}", contentLength);
            log.LogInformation(Encoding.Default.GetString(reqBody, 0, contentLength));

            using (Aes cypher = Aes.Create())
            {
                string key = "abcdefghijklmnop";
                // byte[] buf = Encoding.ASCII.GetBytes(requestBody);
                byte[] buf = reqBody;
                string decrypted = DecryptStringFromBytes_Aes(buf, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(key));
                log.LogInformation(decrypted);
                requestBody = decrypted;
            }

            var reqData = JsonConvert.DeserializeObject(requestBody);

            switch (req.Method) {
                case "PUT":
                    result = await BlobRoutine.putData(reqData, storageBlob);
                    return new OkObjectResult(result);
                
                case "POST":
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
                // aesAlg.Padding = PaddingMode.None;
                aesAlg.Padding = PaddingMode.PKCS7;
                // aesAlg.Mode = CipherMode.CBC;
                aesAlg.BlockSize = 128;
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                // aesAlg.Padding = PaddingMode.Zeros;

                log.LogInformation(Encoding.Default.GetString(aesAlg.Key));
                log.LogInformation(Encoding.Default.GetString(aesAlg.IV));
                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    byte[] pre_cipher = msDecrypt.ToArray();
                    // plaintext = Encoding.Default.GetString(plainTextArr)
                    log.LogInformation("pre cipher length - {0}", pre_cipher.Length);
                    log.LogInformation(Encoding.Default.GetString(pre_cipher));

                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {
                        try {
                            csDecrypt.Write(cipherText);
                        }
                        catch (Exception e) {
                            log.LogInformation(e.ToString());
                        }
                        // using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        // {

                        //     // Read the decrypted bytes from the decrypting stream
                        //     // and place them in a string.
                        //     byte[] plTxt = msDecrypt.
                        //     plaintext = srDecrypt.ReadToEnd();
                        // }

                    }
                    try 
                    {
                        byte[] plainTextArr = msDecrypt.ToArray();

                        for (int i = plainTextArr.Length - 1; i > 0; i--) {
                            if (plainTextArr[i] != 0) {
                                // log.LogInformation("null char - {0}", 0);
                                // log.LogInformation("padding ends with {0} at {1}", plainTextArr[i], i);
                                log.LogInformation("message ends with {0} at {1}", plainTextArr[i], i);
                                log.LogInformation((plainTextArr.Length - i).ToString());
                                break;
                            }
                        }
                        // for (int i = )
                        plaintext = Encoding.Default.GetString(plainTextArr);
                    }
                    catch (Exception e)
                    {
                        log.LogInformation(e.ToString());
                    }
                }
            }

            return plaintext;
        }
    }
}
