using System;

namespace Agent.Abstract.Models
{
    public class AgentMessage
    {
        public DateTime SendDate { get; set; }

        public MessageType MessageType { get; set; }
        
        public Author Author { get; set; }

        public object Data { get; set; }

        public AgentMessage<T> To<T>() where T : class
        {
            return new AgentMessage<T>
            {
                Author = Author,
                MessageType = MessageType,
                SendDate = SendDate,
                Data = Data.ToObject<T>()
            };
        }
    }

    public class AgentMessage<T> : AgentMessage where T : class
    {
        public new T Data { get=> base.Data as T; set=> base.Data = value; }
    }
}