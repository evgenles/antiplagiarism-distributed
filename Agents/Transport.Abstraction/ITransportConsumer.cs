using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Transport.Abstraction
{
    public interface ITransportConsumer
    {
        void Subscribe(string id, string rpcQueueTopic, params string[] queueTopic);

        void Subscribe(string id, string rpcQueueTopic, CancellationToken cancellationToken,
            params string[] queueTopic);
        event ConsumedEventHandler OnConsumed;
        event RpcRequestEventHandler OnRpcRequest;

        public delegate Task ConsumedEventHandler(string agentId, byte[] result, string queueTopic, bool forceBytes, Dictionary<string, string> headers);

        public delegate Task<string> RpcRequestEventHandler(string result, string queueTopic);

        //
        // ConsumeResult<string> Consume();
        // ConsumeResult<string> Consume(TimeSpan timeout);
        // ConsumeResult<T> Consume<T>();
        // ConsumeResult<T> Consume<T>(TimeSpan timeout);
        // void Commit();
    }
}