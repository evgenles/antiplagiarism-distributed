using System;

namespace AgentLoader.Models
{
    public class AgentMessage
    {
        public DateTime SendDate { get; set; }

        public MessageType MessageType { get; set; }
        
        public Author Author { get; set; }

        public object Data { get; set; }
        
        
    }
}