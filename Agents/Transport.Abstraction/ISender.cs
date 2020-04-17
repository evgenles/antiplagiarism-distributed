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
        /// <param name="data">Data bytes</param>
        /// <param name="headers">Headers for send</param>
        /// <param name="forceBytes">Force bytes processing instead of deserealization</param>
        /// <returns>Is success sending</returns>
        public ValueTask<bool> SendAsync(string receiver, byte[] data, bool forceBytes = true, Dictionary<string, string> headers = null);

        /// <summary>
        /// Send data without waiting response
        /// </summary>
        /// <param name="receiver">Whose must receive this message</param>
        /// <param name="data">Serialized message</param>
        /// <param name="forceBytes">Force bytes processing instead of deserealization</param>
        /// <param name="headers">Headers for send</param>
        /// <returns>Is success sending</returns>
        public ValueTask<bool> SendAsync(string receiver, string data, bool forceBytes = false, Dictionary<string, string> headers = null);


        /// <summary>
        /// Send data without waiting response
        /// </summary>
        /// <param name="receiver">Whose must receive this message</param>
        /// <param name="data">Serialized message</param>
        /// <param name="headers">Headers for send</param>
        /// <param name="forceBytes">Force bytes processing instead of deserealization</param>
        /// <returns>Is success sending</returns>
        public ValueTask<bool> SendAsync<T>(string receiver, T data, bool forceBytes = false, Dictionary<string, string> headers = null);

    }
}