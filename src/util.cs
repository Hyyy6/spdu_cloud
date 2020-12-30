using System;

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
}