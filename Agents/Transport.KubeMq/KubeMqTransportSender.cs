﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KubeMQ.SDK.csharp.CommandQuery;
using KubeMQ.SDK.csharp.CommandQuery.LowLevel;
using KubeMQ.SDK.csharp.Events.LowLevel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transport.Abstraction;
using Request = KubeMQ.SDK.csharp.CommandQuery.LowLevel.Request;

namespace Transport.KubeMq
{
    public class KubeMqTransportSender : ITransportSender
    {
        private readonly ILogger<KubeMqTransportSender> _logger;
        private readonly Sender _sender;
        private readonly Initiator _initiator;
        private readonly string _id;

        public KubeMqTransportSender(ILogger<KubeMqTransportSender> logger)
        {
            _logger = logger;
            _sender = new Sender(logger);
            _initiator = new Initiator(logger);
            _id = $"{Environment.MachineName}_{Guid.NewGuid()}";
        }


        public ValueTask<bool> SendAsync(string receiver, byte[] data,  bool forceBytes = true,
            Dictionary<string, string> headers = null)
        {
            try
            {
                Event evt = new Event()
                {
                    Channel = receiver,
                    ClientID = _id,
                    Body = data,
                    Tags = headers,
                    Store = true
                };
                if (forceBytes) evt.Tags.Add("ForceBytes", "1");
                _sender.SendEvent(evt);
                return new ValueTask<bool>(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t send to kubemq");
                return new ValueTask<bool>(true);
            }
        }

        public ValueTask<bool> SendAsync(string receiver, string data, bool forceBytes = false,
            Dictionary<string, string> headers = null)
        {
            return SendAsync(receiver, Encoding.UTF8.GetBytes(data), forceBytes, headers);
        }

        public ValueTask<bool> SendAsync<T>(string receiver, T data, bool forceBytes = false,
            Dictionary<string, string> headers = null)
        {
            return SendAsync(receiver, JsonSerializer.Serialize(data), forceBytes, headers);
        }


        public async Task<string> CallServiceAsync(string receiver, string data, TimeSpan timeOut)
        {
            try
            {
                var request = new Request()
                {
                    Channel = receiver,
                    Metadata = "",
                    Body = Encoding.UTF8.GetBytes(data),
                    Timeout = (int) timeOut.TotalMilliseconds,
                    RequestType = RequestType.Query,
                    ClientID = _id,
                };
                var response = await _initiator.SendRequestAsync(request);
                return Encoding.UTF8.GetString(response.Body);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can`t get response from rpc");
            }

            return null;
        }

        public Task<string> CallServiceAsync(string receiver, string data)
        {
            return CallServiceAsync(receiver, data, TimeSpan.FromSeconds(30));
        }

        public async ValueTask<TResp> CallServiceAsync<TReq, TResp>(string receiver, TReq data)
        {
            var result = await CallServiceAsync(receiver, JsonSerializer.Serialize(data));
            if (!string.IsNullOrEmpty(result))
            {
                return JsonSerializer.Deserialize<TResp>(result);
            }

            return default;
        }

        public async ValueTask<TResp> CallServiceAsync<TReq, TResp>(string receiver, TReq data, TimeSpan timeOut)
        {
            var result = await CallServiceAsync(receiver, JsonSerializer.Serialize(data), timeOut);
            if (!string.IsNullOrEmpty(result))
            {
                return JsonSerializer.Deserialize<TResp>(result);
            }

            return default;
        }
    }
}