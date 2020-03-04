using System;

namespace Agent.Abstract.Models
{
    public class RpcRequest
    {
        public AgentType RequestedAgent { get; set; }

        public RpcRequestType Type { get => Enum.Parse<RpcRequestType>(TypeStr); set => TypeStr = value.ToString(); }
        public string TypeStr { get; set; }

        public string[] Args { get; set; }
    }
}