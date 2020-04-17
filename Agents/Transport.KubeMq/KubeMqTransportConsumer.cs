using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KubeMQ.SDK.csharp.CommandQuery;
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
                if (OnConsumed != null)
                {
                    await OnConsumed(eventReceive.Body, eventReceive.Channel,
                        eventReceive.Tags.ContainsKey("ForceBytes"),
                        eventReceive.Tags
                    );
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t process incoming event");
            }
        }

        private Response HandleRpcRequest(RequestReceive eventReceive, string id)
        {
            try
            {
                var result = OnRpcRequest(Encoding.UTF8.GetString(eventReceive.Body), eventReceive.Channel)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                return new Response(eventReceive)
                {
                    Body = Encoding.UTF8.GetBytes(result),
                    CacheHit = false,
                    ClientID = $"{id}{Guid.NewGuid()}",
                    Error = "",
                    Executed = true,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t process incoming event");
                return new Response(eventReceive)
                {
                    Error = e.ToString(),
                    ClientID = $"{id}{Guid.NewGuid()}",
                    Body = new byte[0]
                };
            }
        }

        public void Subscribe(string id, string rpcQueueTopic, params string[] queueTopic)
        {
            Subscribe(id, rpcQueueTopic, CancellationToken.None, queueTopic);
        }

        public void Subscribe(string id, string rpcQueueTopic, CancellationToken cancellationToken,
            params string[] queueTopic)
        {
            try
            {
                Subscriber subscriber = new Subscriber(_logger);
                foreach (var channel in queueTopic)
                {
                    SubscribeRequest subscribeRequest = new SubscribeRequest(SubscribeType.Events,
                        $"{id}{Guid.NewGuid()}", channel, EventsStoreType.Undefined, 0, id);
                    subscriber.SubscribeToEvents(subscribeRequest, HandleIncomingEvents, ErrorDelegate,
                        cancellationToken);
                }

                Responder responder = new Responder();
                SubscribeRequest rpcSubscribeRequest = new SubscribeRequest(SubscribeType.Queries,
                    $"{id}{Guid.NewGuid()}", rpcQueueTopic, EventsStoreType.Undefined, 0, id);
                responder.SubscribeToRequests(rpcSubscribeRequest, (request) => HandleRpcRequest(request, id),
                    ErrorDelegate, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t subscribe to events");
            }
        }

        public event ITransportConsumer.ConsumedEventHandler OnConsumed;
        public event ITransportConsumer.RpcRequestEventHandler OnRpcRequest;
    }
}