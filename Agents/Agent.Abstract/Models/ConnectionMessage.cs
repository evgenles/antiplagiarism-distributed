namespace Agent.Abstract.Models
{
    public class ConnectionMessage
    {
        public Author Who { get; set; }

        public AgentState State { get; set; }
    }
}