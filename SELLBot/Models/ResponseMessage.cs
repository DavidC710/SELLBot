using System.Net;

namespace SELLBot.Models
{
    public class ResponseMessage
    {
        public string Message { get; set; } 
        public HttpStatusCode Status { get; set; }
    }
}
