using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private async void HandleIncomingEvents(EventReceive eventReceive, string id)
        {
            try
            {
                if (OnConsumed != null)
                {
                    await OnConsumed(id, eventReceive.Body, eventReceive.Channel,
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
                if (OnRpcRequest != null)
                {
                    foreach (var inv in OnRpcRequest.GetInvocationList())
                    {
                        var result = ((Task<string>) inv
                                .DynamicInvoke(Encoding.UTF8.GetString(eventReceive.Body),
                                    eventReceive.Channel))?.ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();
                        if (result != null)
                        {
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
                    }
                }

                return new Response(eventReceive)
                {
                    Error = "Cant find rpc agent",
                    ClientID = $"{id}{Guid.NewGuid()}",
                    Body = new byte[0]
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

        public void Subscribe(string id, string rpcQueueTopic, Dictionary<string, CancellationToken> queueTopic)
        {
            Subscribe(id, rpcQueueTopic, CancellationToken.None, queueTopic);
        }

        public void SubscribeOne(string id, string topic, CancellationToken token)
        {
            try
            {
                Subscriber subscriber = new Subscriber(_logger);
                SubscribeRequest subscribeRequest = new SubscribeRequest(SubscribeType.EventsStore,
                    $"{id}{Guid.NewGuid()}", topic, EventsStoreType.StartNewOnly, 0, id);
                subscriber.SubscribeToEvents(subscribeRequest, r => HandleIncomingEvents(r, id), ErrorDelegate,
                    token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t subscribe to events");
            }
        }

        public void Subscribe(string id, string rpcQueueTopic, CancellationToken cancellationToken,
            Dictionary<string, CancellationToken> queueTopic)
        {
            try
            {
                Subscriber subscriber = new Subscriber(_logger);
                foreach (var (channel, token) in queueTopic)
                {
                    SubscribeRequest subscribeRequest = new SubscribeRequest(SubscribeType.EventsStore,
                        $"{id}{Guid.NewGuid()}", channel, EventsStoreType.StartNewOnly, 0, id);
                    subscriber.SubscribeToEvents(subscribeRequest, r => HandleIncomingEvents(r, id), ErrorDelegate,
                        token);
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