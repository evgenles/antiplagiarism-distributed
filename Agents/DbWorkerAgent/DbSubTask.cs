using System;
using System.Collections.Generic;
using Agent.Abstract.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace DbWorkerAgent
{
    public class DbSubTask
    {
        [BsonId] public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime StartDate { get; set; }

        public TaskState State { get; set; }

        public double ProcessPercentage { get; set; }

        public double UniquePercentage { get; set; }

        public double ErrorPercentage { get; set; }
        
        public Dictionary<string, WorkerInfo> Workers { get; set; }
    }
}