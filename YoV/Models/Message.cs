using System;
namespace YoV.Models
{
    public class Message
    {
        public enum MessageDirection
        {
            INCOMING,
            OUTGOING
        }

        public string User { get; set; }
        public string Content { get; set; }
        public MessageDirection Direction { get; set; }
    }
}
