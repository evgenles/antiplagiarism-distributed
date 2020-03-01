using System;
using System.Collections.Generic;

namespace Agent.Abstract.Models
{
    public class TaskMessage
    {
        public string Creator { get; set; }
        
        public Guid Id { get; set; }
        
        public string Name { get; set; }

        public DateTime StartDate { get; set; }

        public List<string> RequiredSubtype { get; set; }
    }
}