using System.Collections.Generic;
using Agent.Abstract.Models;

namespace Ui.Shared
{
    public class TaskWithSubTasks : TaskMessage
    {
        public List<TaskWithSubTasks> Children { get; set; }
    }
}