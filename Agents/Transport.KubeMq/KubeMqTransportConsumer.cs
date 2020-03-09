using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using KubeMQ.SDK.csharp.Events;
using KubeMQ.SDK.csharp.Subscription;
using Microsoft.Extensions.Logging;
using Transport.Abstraction;

namespace Transport.KubeMq
{
    public class KubeMqTransportConsumer : ITransportConsumer
    {
        private readonly ILogger<KubeMqTransportConsumer> _logger;

        public KubeMqTransportConsumer(ILogger<KubeMqTransportConsumer> logger)
        {
            _logger = logger;
        }

        private void ErrorDelegate(Exception eventReceive)
        {
            _logger.LogError(eventReceive, "Error excepted while consuming from kubeMQ");
        }

        private async void HandleIncomingEvents(EventReceive eventReceive)
        {
            try
            {
                await OnConsumed(Encoding.UTF8.GetString(eventReceive.Body), eventReceive.Channel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t process incoming event");
            }
        }


        public void Subscribe(string id, string rpcQueueTopic, params string[] queueTopic)
        {
            Subscribe(id, rpcQueueTopic, CancellationToken.None, queueTopic);
        }

        public void Subscribe(string id, string rpcQueueTopic, CancellationToken cancellationToken,
            params string[] queueTopic)
        {
            Subscriber subscriber = new Subscriber();
            foreach (var channel in queueTopic)
            {
                SubscribeRequest subscribeRequest = new SubscribeRequest(SubscribeType.Events,
                    $"{id}{Guid.NewGuid()}", channel, EventsStoreType.Undefined, 0, id);
                subscriber.SubscribeToEvents(subscribeRequest, HandleIncomingEvents, ErrorDelegate, cancellationToken);
            }
        }

        public event ITransportConsumer.ConsumedEventHandler OnConsumed;
        public event ITransportConsumer.RpcRequestEventHandler OnRpcRequest;
    }
}