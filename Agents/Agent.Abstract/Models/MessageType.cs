namespace Agent.Abstract.Models
{
    public enum MessageType
    {
        Unknown,
        Connection,
        ConnectionRequest,
        RpcResponse,
        Task,
        TaskRequest,
        TaskStat,
        TaskStatRequest,

    }
}