using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agent.Abstract;
using Agent.Abstract.Models;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Transport.Abstraction;

namespace FileWorkerAgent
{
    public class FileWorkerAgentImpl : AgentAbstract, IFileWorkerAgent
    {
        private readonly ILogger<FileWorkerAgentImpl> _logger;
        private readonly IGridFSBucket _gridFs;

        public FileWorkerAgentImpl(IGridFSBucket gridFsBucket, ITransportSender transport,
            ILogger<FileWorkerAgentImpl> logger) : base(transport,
            AgentType.FileManager, "", MessageType.FileRequest, MessageType.Unknown)
        {
            _logger = logger;
            _gridFs = gridFsBucket;
        }

        public FileWorkerAgentImpl(ITransportSender transport, ILogger<FileWorkerAgentImpl> logger) : base(transport,
            AgentType.FileManager, "", MessageType.FileRequest, MessageType.DeleteFile)
        {
            _logger = logger;
            var client = new MongoClient("mongodb://root:example@localhost");
            var database = client.GetDatabase("tasks");
            _gridFs = new GridFSBucket(database);
        }

        public override async Task ProcessMessageAsync(AgentMessage message)
        {
            if (message.MessageType == MessageType.DeleteFile)
            {
                var taskId = message.To<string>().Data;
                await DeleteFileAsync(taskId);
            }
        }

        public override async Task ProcessMessageAsync(byte[] clearByteMessage, Dictionary<string, string> headers)
        {
            if (headers.TryGetValue("Task", out string taskId))
            {
                await UploadFileAsync(taskId, clearByteMessage);
            }
            else
            {
                _logger.LogError("Cant get TaskId in {@AgentName} while uploading file", nameof(FileWorkerAgentImpl));
            }
        }

        public override async Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            if (message.MessageType == MessageType.FileRequest && message.Data.Args.Length > 0)
            {
                try
                {
                    var response = await GetFileAsync(message.Data.Args[0]);
                    var rpcResponse = new AgentMessage<byte[]>
                    {
                        Author = this,
                        Data = response,
                        MessageType = MessageType.RpcResponse
                    };
                    return rpcResponse;
                }
                catch (Exception e)
                {
                    _logger.LogError("Error occured while downloading {@TaskId} in {@AgentName} with {@Exception}",
                        message.Data.Args[0], nameof(FileWorkerAgentImpl), e.ToString());
                }
            }

            return null;
        }

        public async Task DeleteFileAsync(string taskId)
        {
            try
            {
                var files = await _gridFs.FindAsync(
                    new ExpressionFilterDefinition<GridFSFileInfo>(x => x.Filename == taskId));
                var file = await files.FirstOrDefaultAsync();
                if (file != null)
                    await _gridFs.DeleteAsync(file.Id);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while uploading {@TaskId} in {@AgentName} with {@Exception}",
                    taskId, nameof(FileWorkerAgentImpl), e.ToString());
            }
        }

        public async Task UploadFileAsync(string taskId, byte[] data)
        {
            try
            {
                await _gridFs.UploadFromBytesAsync(taskId, data);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while uploading {@TaskId} in {@AgentName} with {@Exception}",
                    taskId, nameof(FileWorkerAgentImpl), e.ToString());
            }
        }

        public async Task<byte[]> GetFileAsync(string taskId)
        {
            var response = await _gridFs.DownloadAsBytesByNameAsync(taskId);
            return response;
        }
    }
}