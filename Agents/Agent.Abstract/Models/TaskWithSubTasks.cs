using System.Collections.Generic;

namespace Agent.Abstract.Models
{
    public class TaskWithSubTasks : TaskMessage
    {
        public List<TaskWithSubTasks> Children { get; set; }
        public Dictionary<string, WorkerInfo> Workers { get; set; }
    }
}