using System.Collections.Generic;
using System.Threading.Tasks;

namespace Transport.Abstraction
{
    public interface ISender
    {
        /// <summary>
        /// Send data without waiting response
        /// </summary>
        /// <param name="receiver">Whose must receive this message</param>
        /// <param name="data">Serialized message</param>
        /// <param name="headers">Headers for send</param>
        /// <returns>Is success sending</returns>
        public Task<bool> SendAsync(string receiver, string data, Dictionary<string, string> headers = null);


        /// <summary>
        /// Send data without waiting response
        /// </summary>
        /// <param name="receiver">Whose must receive this message</param>
        /// <param name="data">Serialized message</param>♦
        /// <param name="headers">Headers for send</param>
        /// <returns>Is success sending</returns>
        public ValueTask<bool> SendAsync<T>(string receiver, T data, Dictionary<string, string> headers = null);

    }
}