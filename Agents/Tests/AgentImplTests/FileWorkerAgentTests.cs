using System;
using System.Collections.Generic;
using Agent.Abstract.Models;
using FileWorkerAgent;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Xunit;
using ILogger = DnsClient.Internal.ILogger;

namespace AgentImplTests
{
    public class FileWorkerAgentTests
    {
        public (GridFSBucket gridFs, FileWorkerAgentImpl fileAgent) Configure()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<FileWorkerAgentImpl>();
            var mongoClient = new MongoClient("mongodb://root:example@localhost");
            var database = mongoClient.GetDatabase("tasks");
            var gridFs = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = "test"
            });
            var fileAgent = new FileWorkerAgent.FileWorkerAgentImpl(gridFs, null, logger);
            return (gridFs, fileAgent);
        }
        
        [Fact]
        public async void UploadFileRealTest()
        {
            //prepare
            var (gridFs, fileAgent) = Configure();
            var byteData = new byte[] {2, 0, 2, 0, 0, 4, 1, 8, 1, 0, 3, 8};

            var testId = new Guid().ToString("N");
            var f = await gridFs.FindAsync(
                new ExpressionFilterDefinition<GridFSFileInfo<ObjectId>>(x => x.Filename == testId));
            var existFile = await f.FirstOrDefaultAsync();
            if (existFile != null)
            {
                await gridFs.DeleteAsync(existFile.Id);
            }

            //execute
            await fileAgent.ProcessMessageAsync(byteData, new Dictionary<string, string>
            {
                ["Task"] = testId
            });

            //checks
            f = await gridFs.FindAsync(
                new ExpressionFilterDefinition<GridFSFileInfo<ObjectId>>(x => x.Filename == testId));
            var fileInFs = await f.FirstOrDefaultAsync();
            Assert.NotNull(fileInFs);
            Assert.Equal(testId, fileInFs.Filename);
            Assert.Equal(byteData.Length, fileInFs.Length);

            // executeGet
            var data = await fileAgent.ProcessRpcAsync(new AgentMessage<RpcRequest>
            {
                MessageType = MessageType.FileRequest,
                Data = new RpcRequest
                {
                    Args = new[] {testId}
                }
            });
            var resBytes = data.To<byte[]>().Data;
            //checks
            Assert.Equal(resBytes, byteData);

            //clear
            await fileAgent.ProcessMessageAsync(new AgentMessage
            {
                MessageType = MessageType.DeleteFile,
                Data = testId
            });

            //checks
            f = await gridFs.FindAsync(
                new ExpressionFilterDefinition<GridFSFileInfo<ObjectId>>(x => x.Filename == testId));
            Assert.False(await f.AnyAsync());
        }

        [Fact]
        public async void CheckNoExistFileTest()
        {
            var (_, fileAgent) = Configure();
            var data = await fileAgent.ProcessRpcAsync(new AgentMessage<RpcRequest>
            {
                MessageType = MessageType.FileRequest,
                Data = new RpcRequest
                {
                    Args = new[] {Guid.NewGuid().ToString("N")}
                }
            });
            Assert.NotNull(data);
            
            var resBytes = data.To<byte[]>().Data;
            Assert.Null(resBytes);
        }
    }
}