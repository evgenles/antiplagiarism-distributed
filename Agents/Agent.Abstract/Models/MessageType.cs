namespace Agent.Abstract.Models
{
    public enum MessageType
    {
        Unknown,
        Connection,
        ConnectionRequest,
        RpcResponse,
        SplitterTask,
        TaskRequest,
        WorkerTask,
        TaskStat,
        TaskStatRequest,

    }
}