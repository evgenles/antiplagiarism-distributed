using Agent.Abstract;

namespace AgentLoader.Models
{
    public class Author
    {
        public AgentType Type { get; set; }
        
        public string SubType { get; set; }

        public string MachineName { get; set; }

        public string Ip { get; set; }

        public string Version { get; set; }

        public Author(AgentAbstract agent)
        {
            Type = agent.Type;
            Ip = agent.Ip;
            SubType = agent.SubType;
            Version = agent.Version;
            MachineName = agent.MachineName;
        }
        
        public Author(){}
    }
}