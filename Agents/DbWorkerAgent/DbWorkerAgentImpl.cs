using System;
using System.Collections.Generic;
using System.Linq;
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
            AgentType.DbManager, "", MessageType.DbRequest, MessageType.SplitterTask, MessageType.WorkerTask,
            MessageType.TaskStat)
        {
            _logger = logger;
            var client = new MongoClient("mongodb://root:example@localhost");
            _taskCollection = client.GetDatabase("tasks").GetCollection<DbTask>("tasks");
        }

        public override async Task ProcessMessageAsync(AgentMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.SplitterTask:
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
                    break;
                }
                case MessageType.WorkerTask:
                {
                    var taskMsg = message.To<TaskMessage>();
                    _logger.LogInformation($"Save to DB work task {taskMsg.Data.Id}. Parent: {taskMsg.Data.ParentId}");

                    var upd = Builders<DbTask>.Update.Push(x => x.SubTasks, new DbSubTask
                    {
                        Id = taskMsg.Data.Id,
                        Name = taskMsg.Data.Name,
                        State = taskMsg.Data.State,
                        StartDate = taskMsg.Data.StartDate,
                        ErrorPercentage = taskMsg.Data.ErrorPercentage,
                        ProcessPercentage = taskMsg.Data.ProcessPercentage,
                        UniquePercentage = taskMsg.Data.UniquePercentage,
                        Workers = new Dictionary<string, WorkerInfo>()
                    });
                    await _taskCollection.FindOneAndUpdateAsync(x => x.Id == taskMsg.Data.ParentId, upd);
                    break;
                }
                case MessageType.TaskStat:
                {
                    var taskMsg = message.To<TaskMessage>();
                    var upd = Builders<DbTask>.Update
                        .Set(f => f.SubTasks[-1].Workers[taskMsg.Author.SubType].Process, taskMsg.Data.ProcessPercentage)
                        .Set(f => f.SubTasks[-1].Workers[taskMsg.Author.SubType].Errors, taskMsg.Data.ErrorPercentage)
                        .Set(f => f.SubTasks[-1].Workers[taskMsg.Author.SubType].Unique, taskMsg.Data.UniquePercentage);
                    if (taskMsg.Data.StartDate != DateTime.MinValue)
                        upd = upd.Set(f => f.SubTasks[-1].StartDate, taskMsg.Data.StartDate);
                    if (!string.IsNullOrEmpty(taskMsg.Data.Report))
                        upd = upd.Set(f => f.SubTasks[-1].Workers[taskMsg.Author.SubType].Report, taskMsg.Data.Report);
                    await _taskCollection.FindOneAndUpdateAsync(
                        x => x.Id == taskMsg.Data.ParentId && x.SubTasks.Any(st => st.Id == taskMsg.Data.Id),
                        upd);
                    break;
                }
            }
        }


        public override async Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            if (message.Data.Type == RpcRequestType.GetAllTasks)
            {
                var tasks = await _taskCollection.Find(x => true).ToListAsync();
                var taskMsg = tasks.ToDictionary(x=>x.Id.ToString(), x => new TaskWithSubTasks
                {
                    Creator = x.Creator,
                    Id = x.Id,
                    Name = x.Name,
                    State = x.State,
                    ErrorPercentage = x.ErrorPercentage,
                    ProcessPercentage = x.ProcessPercentage,
                    UniquePercentage = x.UniquePercentage,
                    FileName = x.FileName,
                    StartDate = x.StartDate.ToLocalTime(),
                    Children = x.SubTasks?.Select(y => new TaskWithSubTasks
                    {
                        Id = y.Id,
                        Name = y.Name,
                        State = y.State,
                        ErrorPercentage = y.ErrorPercentage,
                        ProcessPercentage = y.ProcessPercentage,
                        UniquePercentage = y.UniquePercentage,
                        StartDate = y.StartDate.ToLocalTime(),
                        Workers = y.Workers
                    }).ToList()
                });
                return new AgentMessage<Dictionary<string, TaskWithSubTasks>>()
                {
                    Author = this,
                    Data = taskMsg,
                    SendDate = DateTime.Now
                };
            }

            return null;
        }
    }
}