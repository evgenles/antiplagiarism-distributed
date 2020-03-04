using System;

namespace Agent.Abstract.Models
{
    public class Author
    {
        public Guid Id { get; set; }
        public AgentType Type { get; set; }
        
        public string SubType { get; set; }

        public string MachineName { get; set; }

        public string Ip { get; set; }

        public string Version { get; set; }

        public static implicit operator Author(AgentAbstract agent)
        {
            return new Author(agent);
        } 
        
        public Author(AgentAbstract agent)
        {
            Type = agent.Type;
            Ip = agent.Ip;
            SubType = agent.SubType;
            Version = agent.Version;
            MachineName = agent.MachineName;
            Id = agent.Id;
        }
        
        public Author(){}
    }
}