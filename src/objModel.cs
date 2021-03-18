using System.Net;
using utils;

namespace objectModels
{
    public class ReqMin
    {
        public string password {get; set;}
    }
    public class SPDURequest : ReqMin
    {
        // public string password {get; set;}
        // public string deviceName {get; set;}
        public string ipAddress {get; set;}
        public string key {get; set;}
    }

    // public class ClientRequest
    // {
    //     public string password {get; set;}
    //     // public string
    // }
    public class BlobEntry
    {
        public string ipAddress {get; set;}
        public string date {get; set;}
        public string key {get; set;}
    }

    // public class SPDUPayload
    // {
    // }
}