using System.Net;
using utils;

namespace objectModels
{
    public class SPDURequest
    {
        public string name {get; set;}
        public string password {get; set;}
        public SPDUPayload payload {get; set;}
    }

    public class SPDUPayload
    {
        public string deviceName {get; set;}
        public string ipAddress {get; set;}
    }
}