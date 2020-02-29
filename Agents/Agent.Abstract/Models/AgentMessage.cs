using System;
using AgentLoader.Models;

namespace Agent.Abstract.Models
{
    public class AgentMessage
    {
        public DateTime SendDate { get; set; }

        public MessageType MessageType { get; set; }
        
        public Author Author { get; set; }

        public object Data { get; set; }
        

    }
}