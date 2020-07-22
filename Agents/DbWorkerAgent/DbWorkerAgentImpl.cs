using System;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Transport.Abstraction;

namespace DbWorkerAgent
{
    public class DbWorkerAgentImpl : AgentAbstract
    {
        private readonly ILogger<DbWorkerAgentImpl> _logger;
        private IMongoCollection<DbTask> _taskCollection;

        public DbWorkerAgentImpl(ITransportSender transport, ILogger<DbWorkerAgentImpl> logger) : base(transport,
            AgentType.DbManager, "", MessageType.DbRequest, MessageType.SplitterTask, MessageType.WorkerTask, MessageType.TaskStat)
        {
            _logger = logger;
            var client = new MongoClient("mongodb://root:example@localhost");
            _taskCollection = client.GetDatabase("tasks").GetCollection<DbTask>("tasks");
        }

        public override async Task ProcessMessageAsync(AgentMessage message)
        {
            if (message.MessageType == MessageType.SplitterTask)
            {
                var taskMsg = message.To<TaskMessage>();
        
                _logger.LogInformation($"Save to DB split task {taskMsg.Data.Id}");
                var task = new DbTask
                {
                    Id = taskMsg.Data.Id,
                    Creator = taskMsg.Data.Creator,
                    Name = taskMsg.Data.Name,
                    State = taskMsg.Data.State,
                    FileName = taskMsg.Data.FileName,
                    StartDate = taskMsg.Data.StartDate,
                    ErrorPercentage = taskMsg.Data.ErrorPercentage,
                    ProcessPercentage = taskMsg.Data.ProcessPercentage,
                    RequiredSubtype = taskMsg.Data.RequiredSubtype,
                    UniquePercentage = taskMsg.Data.UniquePercentage
                };
                await _taskCollection.InsertOneAsync(task);
            }
            else if (message.MessageType == MessageType.WorkerTask)
            {
                var taskMsg = message.To<TaskMessage>();
                _logger.LogInformation($"Save to DB work task {taskMsg.Data.Id}. Parent: {taskMsg.Data.ParentId}");

                var parent = await _taskCollection.Find(x => x.Id == taskMsg.Data.ParentId)
                    .FirstOrDefaultAsync();
                parent.SubTasks.Add(new DbTask
                {
                    Id = taskMsg.Data.Id,
                    Creator = taskMsg.Data.Creator,
                    Name = taskMsg.Data.Name,
                    State = taskMsg.Data.State,
                    FileName = taskMsg.Data.FileName,
                    StartDate = taskMsg.Data.StartDate,
                    ErrorPercentage = taskMsg.Data.ErrorPercentage,
                    ProcessPercentage = taskMsg.Data.ProcessPercentage,
                    RequiredSubtype = taskMsg.Data.RequiredSubtype,
                    UniquePercentage = taskMsg.Data.UniquePercentage
                });
                parent.StartDate = taskMsg.Data.StartDate;

                await _taskCollection.ReplaceOneAsync(x=>x.Id == parent.Id, parent);
            }
        }


        public override async Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            if (message.MessageType == MessageType.FileRequest && message.Data.Args.Length > 0)
            {
               
            }

            return null;
        }
    }
}