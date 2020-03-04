using System;
using System.Threading.Tasks;

namespace Transport.Abstraction
{
    public interface IRpc
    {
        
        /// <summary>
        /// Call another agent with default timeout parameter
        /// </summary>
        /// <param name="receiver">Receiver identifier</param>
        /// <param name="data">serialized send data</param>
        /// <returns>Execution result</returns>
        public Task<string> CallServiceAsync(string receiver, string data);
        
        /// <summary>
        /// Call another agent
        /// </summary>
        /// <param name="receiver">Receiver identifier</param>
        /// <param name="data">serialized send data</param>
        /// <param name="timeOut">operation timeout</param>
        /// <returns>Execution result</returns>
        public Task<string> CallServiceAsync(string receiver, string data, TimeSpan timeOut);

        /// <summary>
        /// Call another agent with default timeout parameter
        /// </summary>
        /// <param name="receiver">Receiver identifier</param>
        /// <param name="data">serialized send data</param>
        /// <returns>Execution result</returns>
        public ValueTask<TResp> CallServiceAsync<TReq, TResp>(string receiver, TReq data);

        /// <summary>
        /// Call another agent
        /// </summary>
        /// <param name="receiver">Receiver identifier</param>
        /// <param name="data">serialized send data</param>
        /// <param name="timeOut">operation timeout</param>
        /// <returns>Execution result</returns>
        public ValueTask<TResp> CallServiceAsync<TReq, TResp>(string receiver, TReq data, TimeSpan timeOut);

    }
}