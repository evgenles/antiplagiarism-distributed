using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Agent.Abstract.Models
{
    public class TaskMessage
    {
        public string Creator { get; set; }
        
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid ParentId { get; set; }
        
        public string Name { get; set; }
        public string FileName { get; set; }

        public DateTime StartDate { get; set; } 

        public TaskState State { get; set; }
        public List<string> RequiredSubtype { get; set; }
        
        public byte[] Data { get; set; }

        public double ProcessPercentage { get; set; }
        
        public double UniquePercentage { get; set; }
        
        public double ErrorPercentage { get; set; }

    }
}