using System;
using System.Collections.Generic;
using System.IO;

namespace Agent.Abstract.Models
{
    public class TaskMessage
    {
        public string Creator { get; set; }
        
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid ParentId { get; set; }
        
        public string Name { get; set; }

        public DateTime StartDate { get; set; } 

        public List<string> RequiredSubtype { get; set; }
        
        public Stream DataStream { get; set; }

        public double ProcessPercentage { get; set; }
        
        public double UniquePercentage { get; set; }
        
        public double ErrorPercentage { get; set; }

    }
}