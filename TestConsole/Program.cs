using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Agent.Abstract.Models;
using DocumentSplitterAgent;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var agent = new DocumentSplitter(null);
            agent.ProcessMessageAsync(new AgentMessage
                {
                    Data = new TaskMessage
                    {
                        //   Data = File.OpenRead("diploma2.docx")
                    }
                }, new Dictionary<string, string>())
                .GetAwaiter()
                .GetResult();
        }
    }
}