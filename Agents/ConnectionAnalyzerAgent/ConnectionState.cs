using System;
using Agent.Abstract.Models;

namespace ConnectionAnalyzerAgent
{
    public class ConnectionState : ConnectionMessage
    {
        public DateTime LastUpdate { get; set; }
    }
}