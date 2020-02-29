using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TestConsole
{
   
    class Program
    {
        static void Main(string[] args)
        {
            var ip = string.Join(", " ,Dns.GetHostAddresses(Dns.GetHostName())
                .Where(x=>x.AddressFamily == AddressFamily.InterNetwork)
                .Select(x=>x.ToString()));

            Console.WriteLine("Hello World!");
        }
    }
}